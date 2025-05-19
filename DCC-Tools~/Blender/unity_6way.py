import bpy
import os
import math
import mathutils
import time

bl_info = {
    "name": "Unity VFX Graph Six way lighting",
    "blender": (3, 3, 0),
    "category": "Import-Export",
}

_restore_info = {}

_light_direction_names = ("Right", "Left", "Bottom", "Top", "Front", "Back")
_node_separation = (200, 100)

_rgba_combiner_node_group_name = "UnityRGBACombinerGroup"
_6way_combiner_node_group_name = "Unity6wayCombinerGroup"

_compositor_debug = False

def _get_frames_range(scene):
    unity6way = scene.unity6way
    match unity6way.frames:
        case 'CURRENT':
            frame_start = scene.frame_start
            frame_end = scene.frame_end
        case 'FRAME':
            frame_start = unity6way.frame_start
            frame_end = unity6way.frame_start
        case 'RANGE':
            frame_start = unity6way.frame_start
            frame_end = unity6way.frame_end
    return frame_start, frame_end

def _get_current_frame(scene):
    frame_start, frame_end = _get_frames_range(scene)
    return max(frame_start, min(frame_end, scene.frame_current))
    
def _create_compositor_node_image_input(tree, image, scene):
    unity6way = scene.unity6way
    input_node = tree.nodes.new(type='CompositorNodeImage')
    input_node.image = image
    if unity6way.frames == 'FRAME':
        image.source = 'FILE'
    else:
        image.source = 'SEQUENCE'
    input_node.use_straight_alpha_output = True
    input_node.frame_offset = scene.frame_start - 1
    input_node.frame_start = scene.frame_start
    input_node.frame_duration = scene.frame_end - scene.frame_start + 1
    return input_node

def _create_compositor_node_exr_output(tree):
    output_node = tree.nodes.new(type='CompositorNodeOutputFile')
    output_node.format.file_format = 'OPEN_EXR'
    output_node.format.color_mode = 'RGBA'
    output_node.format.color_depth = '16'
    output_node.format.quality = 100
    return output_node
    
def _create_compositor_node_exr_multilayer_output(tree):
    output_node = tree.nodes.new(type='CompositorNodeOutputFile')
    output_node.format.file_format = 'OPEN_EXR_MULTILAYER'
    output_node.format.color_mode = 'RGBA'
    output_node.format.color_depth = '16'
    output_node.format.quality = 100
    return output_node

def _create_node_group(tree, group_name, fn):
    if not bpy.data.node_groups.__contains__(group_name):
        fn()

    group_node = tree.nodes.new(type='CompositorNodeGroup')
    group_node.node_tree = bpy.data.node_groups[group_name]
    return group_node

def _get_input_path(directory, filename, extension):
    return "{0}{1}.{2}".format(directory, filename, extension)

def _get_input_path_frame(directory, filename, frame, extension):
    return "{0}{1}{3:04d}.{2}".format(directory, filename, extension, frame)

def _get_format_extension(format):
    match format:
        case 'OPEN_EXR':
            return "exr"
        case 'TARGA':
            return "tga"
        case 'PNG':
            return "png"
    return ""

def _get_lightmaps_path(unity6way, frame):
    return _get_input_path_frame(unity6way.temp_path, unity6way.lightmaps.filename, frame, "exr")

def _get_emissive_path(unity6way, frame):
    return _get_input_path_frame(unity6way.temp_path, unity6way.emissive.filename, frame, "exr")

def _get_compositing_paths(unity6way, frame):
    path1 = _get_input_path_frame(unity6way.temp_path, unity6way.compositing.filename1, frame, "exr")
    path2 = _get_input_path_frame(unity6way.temp_path, unity6way.compositing.filename2, frame, "exr")
    return (path1, path2)

def _get_export_paths(unity6way):
    output_path = unity6way.temp_path if unity6way.flipbook.use_temp else unity6way.flipbook.dest_path
    input_filenames = (unity6way.compositing.filename1, unity6way.compositing.filename2)
    output_filenames = (unity6way.flipbook.filename1, unity6way.flipbook.filename2)
    filenames = input_filenames if unity6way.flipbook.use_filename else output_filenames
    extension = _get_format_extension(unity6way.flipbook.dest_format)
    path1 = _get_input_path(output_path, filenames[0], extension)
    path2 = _get_input_path(output_path, filenames[1], extension)
    return (path1, path2)

def _file_exists(path):
    return os.path.exists(path)

def _check_input_path(missing_paths, path):
    if not _file_exists(path):
        missing_paths.append(path)

def _load_image(path):
    filename = bpy.path.basename(path)
    image = bpy.data.images.get(filename)
    if image != None:
        bpy.data.images.remove(image)
    return bpy.data.images.load(path, check_existing=False)

def _show_image(path, alpha_mode):
    image = _load_image(path)
    image.alpha_mode = alpha_mode
    bpy.ops.render.view_show('INVOKE_DEFAULT')
    image_area = None
    while image_area == None:
        for window in bpy.context.window_manager.windows:
            for area in window.screen.areas:
                if area.type == 'IMAGE_EDITOR':
                    image_area = area
    image_area.spaces.active.image = image
    return image_area

def _report_missing_inputs(operator, missing_paths):
    message = "Input image(s) not found: "
    for missing_path in missing_paths:
        message += "\n" + missing_path
    operator.report({'WARNING'}, message)

def _remove_compositor_node_group(group_name):
    if bpy.data.node_groups.__contains__(group_name):
        bpy.data.node_groups.remove(bpy.data.node_groups[group_name])

def _add_compositor_node_group(group_name):
    return bpy.data.node_groups.new(group_name, 'CompositorNodeTree')

def _add_rgba_combiner_compositor_node_group():
    tree = _add_compositor_node_group(_rgba_combiner_node_group_name)
    
    group_inputs = tree.nodes.new('NodeGroupInput')
    group_inputs.location = (-3 * _node_separation[0], 0)
    tree.inputs.new('NodeSocketColor',"R")
    tree.inputs.new('NodeSocketColor',"G")
    tree.inputs.new('NodeSocketColor',"B")
    tree.inputs.new('NodeSocketColor',"A")
    tree.inputs.new('NodeSocketFloat',"Multiplier")
    tree.inputs.new('NodeSocketFloatFactor',"Premultiplied")

    group_outputs = tree.nodes.new('NodeGroupOutput')
    group_outputs.location = (3 * _node_separation[0], 0)
    tree.outputs.new('NodeSocketColor',"Image")

    rgba_node = tree.nodes.new(type='CompositorNodeCombRGBA')
    rgba_node.location = (-_node_separation[0], 0)

    multiply_node = tree.nodes.new(type='CompositorNodeMixRGB')
    multiply_node.location = (0, 0)
    multiply_node.use_clamp = False
    multiply_node.blend_type = 'MULTIPLY'
    multiply_node.inputs["Fac"].default_value = 1

    premultiply_node = tree.nodes.new(type='CompositorNodePremulKey')
    premultiply_node.location = (_node_separation[0], _node_separation[1])
    premultiply_node.mapping = 'PREMUL_TO_STRAIGHT'

    alphamode_node = tree.nodes.new(type='CompositorNodeMixRGB')
    alphamode_node.location = (2 * _node_separation[0], 0)
    alphamode_node.use_clamp = True
    alphamode_node.blend_type = 'MIX'
    alphamode_node.inputs["Fac"].default_value = 0
    
    location_y = 1.5 * _node_separation[1]
    for rgba_slot in rgba_node.inputs:
        bw_node = tree.nodes.new(type='CompositorNodeRGBToBW')
        bw_node.location = (-2 * _node_separation[0], location_y)
        tree.links.new(bw_node.outputs["Val"], rgba_slot)
        tree.links.new(group_inputs.outputs[rgba_slot.name], bw_node.inputs["Image"])
        location_y -= _node_separation[1]

    tree.links.new(rgba_node.outputs["Image"], multiply_node.inputs[1])
    tree.links.new(group_inputs.outputs["Multiplier"], multiply_node.inputs[2])
    tree.links.new(multiply_node.outputs["Image"], premultiply_node.inputs[0])
    tree.links.new(multiply_node.outputs["Image"], alphamode_node.inputs[2])
    tree.links.new(premultiply_node.outputs[0], alphamode_node.inputs[1])
    tree.links.new(alphamode_node.outputs["Image"], group_outputs.inputs["Image"])
    tree.links.new(group_inputs.outputs["Premultiplied"], alphamode_node.inputs["Fac"])

def _add_6way_combiner_compositor_node_group():
    tree = _add_compositor_node_group(_6way_combiner_node_group_name)
    
    group_inputs = tree.nodes.new('NodeGroupInput')
    group_inputs.location = (-1.5 * _node_separation[0], 0)
    for slot_name in _light_direction_names:
        tree.inputs.new('NodeSocketColor',slot_name)
    tree.inputs.new('NodeSocketColor',"Alpha")
    tree.inputs.new('NodeSocketColor',"Extra")
    tree.inputs.new('NodeSocketFloat',"Lightmap Multiplier")
    tree.inputs.new('NodeSocketFloat',"Extra Multiplier")
    tree.inputs.new('NodeSocketFloatFactor',"Premultiplied")

    group_outputs = tree.nodes.new('NodeGroupOutput')
    group_outputs.location = (2 * _node_separation[0], 0)
    tree.outputs.new('NodeSocketColor',"Positive")
    tree.outputs.new('NodeSocketColor',"Negative")

    positive_node = _create_node_group(tree, _rgba_combiner_node_group_name, _add_rgba_combiner_compositor_node_group)
    positive_node.location = (0, 2 * _node_separation[1])
    
    negative_node = tree.nodes.new(type='CompositorNodeGroup')
    negative_node.node_tree = positive_node.node_tree
    negative_node.location = (0, -2 * _node_separation[1])

    extra_multiply_node = tree.nodes.new(type='CompositorNodeMixRGB')
    extra_multiply_node.location = (0, -0.2 * _node_separation[1])
    extra_multiply_node.use_clamp = False
    extra_multiply_node.blend_type = 'MULTIPLY'
    extra_multiply_node.inputs["Fac"].default_value = 1

    extra_node = tree.nodes.new(type='CompositorNodeSetAlpha')
    extra_node.mode = 'REPLACE_ALPHA'
    extra_node.location = (_node_separation[0], -0.5 * _node_separation[1])

    tree.links.new(group_inputs.outputs["Right" ], positive_node.inputs["R"])
    tree.links.new(group_inputs.outputs["Left"  ], negative_node.inputs["R"])
    tree.links.new(group_inputs.outputs["Top"   ], positive_node.inputs["G"])
    tree.links.new(group_inputs.outputs["Bottom"], negative_node.inputs["G"])
    tree.links.new(group_inputs.outputs["Back"  ], positive_node.inputs["B"])
    tree.links.new(group_inputs.outputs["Front" ], negative_node.inputs["B"])
    tree.links.new(group_inputs.outputs["Alpha" ], positive_node.inputs["A"])
    tree.links.new(group_inputs.outputs["Alpha" ], negative_node.inputs["A"])
    tree.links.new(group_inputs.outputs["Lightmap Multiplier"], positive_node.inputs["Multiplier"])
    tree.links.new(group_inputs.outputs["Lightmap Multiplier"], negative_node.inputs["Multiplier"])
    tree.links.new(group_inputs.outputs["Premultiplied"], positive_node.inputs["Premultiplied"])
    tree.links.new(group_inputs.outputs["Premultiplied"], negative_node.inputs["Premultiplied"])

    tree.links.new(group_inputs.outputs["Extra"], extra_multiply_node.inputs[1])
    tree.links.new(group_inputs.outputs["Extra Multiplier"], extra_multiply_node.inputs[2])
    tree.links.new(negative_node.outputs["Image"], extra_node.inputs["Image"])
    tree.links.new(extra_multiply_node.outputs["Image"], extra_node.inputs["Alpha"])

    tree.links.new(positive_node.outputs["Image"], group_outputs.inputs["Positive"])
    tree.links.new(extra_node.outputs["Image"], group_outputs.inputs["Negative"])

def _destroy_compositor_nodes(tree, nodes):
    if _compositor_debug:
        return

    for node in nodes:
        tree.nodes.remove(node)


def _on_render_init(scene):
    scene.unity6way.is_rendering = True

def _on_render_cancel(scene):
    scene.unity6way.is_rendering = False

def _on_render_complete(scene):
    scene.unity6way.is_rendering = False

class Unity6Way:
    
    class Panel(bpy.types.Panel):
        bl_idname = "VIEW3D_PT_unity_6way"
        bl_label = "6-Way lighting"
        bl_space_type = 'VIEW_3D'
        bl_region_type = 'UI'
        bl_category = "Unity"

        def draw(self, context):
            scene = context.scene
            unity6way = scene.unity6way

            self.layout.prop(unity6way, "temp_path")
            self.layout.prop(unity6way, "frames", expand=True)

            row = self.layout.row()
            row.enabled = unity6way.frames != 'CURRENT'
            row.prop(unity6way, "frame_start")

            row = self.layout.row()
            row.enabled = unity6way.frames == 'RANGE'
            row.prop(unity6way, "frame_end")

            self.layout.operator(Unity6Way.RenderAllOperator.bl_idname)

    class Lightmaps:

        class Properties(bpy.types.PropertyGroup):
            enabled: bpy.props.BoolProperty(
                name = "Enabled",
                default = True,
            )
            filename : bpy.props.StringProperty(
                name="Filename",
                description = "Filename",
                default="Lightmaps",
            )
            light_angle: bpy.props.FloatProperty(
                name = "Light Angle",
                description = "Light Angle",
                default = 0,
                min = 0,
                max = 90
            )

        class Panel(bpy.types.Panel):
            bl_idname = "VIEW3D_PT_unity_6way_lightmaps"
            bl_parent_id = "VIEW3D_PT_unity_6way"
            bl_label = "Lightmaps"
            bl_space_type = 'VIEW_3D'
            bl_region_type = 'UI'
            bl_options = {'DEFAULT_CLOSED'}

            def draw_header(self, context):
                self.layout.prop(context.scene.unity6way.lightmaps, "enabled", text="")
            
            def draw(self, context):
                scene = context.scene
                unity6way = scene.unity6way
                self.layout.prop(unity6way.lightmaps, "filename")
                self.layout.prop(unity6way.lightmaps, "light_angle")                
                render_operator = self.layout.operator(Unity6Way.RenderUndoOperator.bl_idname)
                render_operator.prepare_operator = "unity_6way_lightmap_prepare"
                render_operator.restore_operator = "unity_6way_lightmap_restore"
                row = self.layout.row()
                dest_path = _get_lightmaps_path(unity6way, _get_current_frame(scene))
                row.enabled = _file_exists(dest_path)
                row.operator(Unity6Way.Lightmaps.ViewResultOperator.bl_idname)

        class PrepareOperator(bpy.types.Operator):
            """Unity VFX Graph Six way setup lighting"""    #tooltip
            bl_idname = "render.unity_6way_lightmap_prepare"
            bl_label = "Prepare lightmaps"
            bl_options = {'REGISTER', 'UNDO'}

            __name_prefix = "unity_6way"

            def create_lights(self, parent_collection, camera, light_angle):
                light_data = bpy.data.lights.new(name=self.__name_prefix+"_light", type='SUN')
                light_data.energy = math.pi
                light_data.angle = math.radians(light_angle)
                light_data.specular_factor = 0

                lights = {}
                for dir_name in _light_direction_names:
                    collection = bpy.data.collections.new(self.__name_prefix + dir_name)
                    parent_collection.children.link(collection)
                    light = bpy.data.objects.new(name=self.__name_prefix+dir_name, object_data=light_data)
                    light.rotation_mode = 'QUATERNION'
                    collection.objects.link(light)
                    lights[dir_name] = light

                if camera.rotation_mode == 'QUATERNION':
                    camera_rotation = camera.rotation_quaternion
                elif camera.rotation_mode == 'AXIS_ANGLE':
                    camera_rotation = camera.rotation_axis_angle.to_quaternion()
                else:
                    camera_rotation = camera.rotation_euler.to_quaternion()

                pi_2 = math.pi * 0.5
                lights["Front" ].rotation_quaternion = camera_rotation 
                lights["Back"  ].rotation_quaternion = camera_rotation @ mathutils.Quaternion([0, 1, 0], math.pi)
                lights["Left"  ].rotation_quaternion = camera_rotation @ mathutils.Quaternion([0, 1, 0], -pi_2) 
                lights["Right" ].rotation_quaternion = camera_rotation @ mathutils.Quaternion([0, 1, 0], pi_2) 
                lights["Top"   ].rotation_quaternion = camera_rotation @ mathutils.Quaternion([1, 0, 0], -pi_2) 
                lights["Bottom"].rotation_quaternion = camera_rotation @ mathutils.Quaternion([1, 0, 0], pi_2)
                _restore_info["lights"] = lights

            def create_layers(self, scene_layers):
                view_layers = {}
                for dir_name in _light_direction_names:
                    layer = scene_layers.new(self.__name_prefix+dir_name)
                    #disable other light collections
                    for other_dir_name in _light_direction_names:
                        layer.layer_collection.children[self.__name_prefix+other_dir_name].exclude = True
                    #enable matching light collection
                    layer.layer_collection.children[layer.name].exclude = False
                    #add to layers
                    view_layers[dir_name] = layer
                _restore_info["view_layers"] = view_layers

            def create_compositor_nodes(self, tree, output_path):
                layer_nodes = {}
                for dir_name in _light_direction_names:
                    layer_node = tree.nodes.new(type='CompositorNodeRLayers')
                    layer_node.layer = self.__name_prefix+dir_name
                    layer_nodes[dir_name] = layer_node

                output_node = _create_compositor_node_exr_multilayer_output(tree)
                output_node.base_path = output_path

                output_node.file_slots.remove(output_node.inputs[0])

                for dir_name in _light_direction_names:
                    output_node.file_slots.new(dir_name)
                    tree.links.new(layer_nodes[dir_name].outputs["Image"], output_node.inputs[dir_name])

                output_node.file_slots.new("Alpha")
                tree.links.new(layer_nodes[_light_direction_names[0]].outputs["Alpha"], output_node.inputs["Alpha"])

                layer_nodes["Left"  ].location = (-4 * _node_separation[0], 4 * _node_separation[1])
                layer_nodes["Right" ].location = (-2 * _node_separation[0], 4 * _node_separation[1])
                layer_nodes["Bottom"].location = (-4 * _node_separation[0], 0)
                layer_nodes["Top"   ].location = (-2 * _node_separation[0], 0)
                layer_nodes["Front" ].location = (-4 * _node_separation[0], -4 * _node_separation[1])
                layer_nodes["Back"  ].location = (-2 * _node_separation[0], -4 * _node_separation[1])

                nodes = []
                for layer_node in layer_nodes.values():
                    nodes.append(layer_node)
                nodes.append(output_node)
                _restore_info["nodes"] = nodes

            def disable_emissive_materials(self):
                restore_emissive_infos = []
                for material in bpy.data.materials:
                    if material.use_nodes:
                        zero_value_node = material.node_tree.nodes.new(type='ShaderNodeValue')
                        zero_value_node.outputs[0].default_value = 0

                        socket_pairs = []
                        for node in material.node_tree.nodes:
                            emission_input = None
                            if node.type == 'ShaderNodeEmission':
                                emission_input = node.inputs["Strength"]
                            elif node.inputs.__contains__("Emission Strength"):
                                emission_input = node.inputs["Emission Strength"]
                            if emission_input != None:
                                if emission_input.is_linked:
                                    socket_pairs.append((emission_input.links[0].from_socket, emission_input))
                                material.node_tree.links.new(zero_value_node.outputs[0], emission_input)

                        restore_emissive_infos.append((material, zero_value_node, socket_pairs))
                _restore_info["emissive_materials"] = restore_emissive_infos

            def execute(self, context):
                scene = context.scene
                unity6way = scene.unity6way
                self.create_lights(scene.collection, scene.camera, unity6way.lightmaps.light_angle)
                self.create_layers(scene.view_layers)
                self.create_compositor_nodes(scene.node_tree, unity6way.temp_path+"\\"+unity6way.lightmaps.filename)
                self.disable_emissive_materials()
                return {'FINISHED'}     

        class RestoreOperator(bpy.types.Operator):
            """Unity VFX Graph Six way setup lighting"""    #tooltip
            bl_idname = "render.unity_6way_lightmap_restore"
            bl_label = "Restore lightmap"
            bl_options = {'REGISTER', 'UNDO'}

            __name_prefix = "unity_6way"

            def destroy_lights(self, lights):
                light_data = lights[_light_direction_names[0]].data
                for light in lights.values():
                    bpy.data.collections.remove(bpy.data.collections[light.name])
                bpy.data.lights.remove(light_data)

            def destroy_layers(self, scene_layers, view_layers):
                for layer in view_layers.values():
                    scene_layers.remove(layer)

            def restore_emissive_materials(self, restore_emissive_infos):
                for restore_emissive_info in restore_emissive_infos:
                    material = restore_emissive_info[0]
                    zero_value_node = restore_emissive_info[1]
                    socket_pairs = restore_emissive_info[2]
                    material.node_tree.nodes.remove(zero_value_node)
                    for socket_pair in socket_pairs:
                        material.node_tree.links.new(socket_pair[0], socket_pair[1])

            def execute(self, context):
                scene = context.scene
                unity6way = scene.unity6way
                self.restore_emissive_materials(_restore_info["emissive_materials"])
                _destroy_compositor_nodes(scene.node_tree, _restore_info["nodes"])
                self.destroy_layers(scene.view_layers, _restore_info["view_layers"])
                self.destroy_lights(_restore_info["lights"])
                return {'FINISHED'}

        class ViewResultOperator(bpy.types.Operator):
            """Unity VFX Graph Six way render lighting"""    #tooltip
            bl_idname = "render.unity_6way_lightmap_view"
            bl_label = "View last result"
            bl_options = {'REGISTER', 'UNDO'}

            def execute(self, context):
                scene = context.scene
                unity6way = scene.unity6way
                filename = _get_lightmaps_path(unity6way, _get_current_frame(scene))
                _show_image(filename, 'PREMUL')
                return {'FINISHED'}

    class Emissive:

        class Properties(bpy.types.PropertyGroup):
            enabled: bpy.props.BoolProperty(
                name = "Enabled",
                default = False,
            )
            filename : bpy.props.StringProperty(
                name="Filename",
                description = "Filename",
                default="Emissive",
            )
            
        class Panel(bpy.types.Panel):
            bl_idname = "VIEW3D_PT_unity_6way_emissive"
            bl_parent_id = "VIEW3D_PT_unity_6way"
            bl_label = "Emissive"
            bl_space_type = 'VIEW_3D'
            bl_region_type = 'UI'
            bl_options = {'DEFAULT_CLOSED'}

            def draw_header(self, context):
                self.layout.prop(context.scene.unity6way.emissive, "enabled", text="")

            def draw(self, context):
                scene = context.scene
                unity6way = scene.unity6way
                self.layout.prop(unity6way.emissive, "filename")
                render_operator = self.layout.operator(Unity6Way.RenderUndoOperator.bl_idname)
                render_operator.prepare_operator = "unity_6way_emissive_prepare"
                render_operator.restore_operator = "unity_6way_emissive_restore"

                row = self.layout.row()
                dest_path = _get_emissive_path(unity6way, _get_current_frame(scene))
                row.enabled = _file_exists(dest_path)
                row.operator(Unity6Way.Emissive.ViewResultOperator.bl_idname)

        class PrepareOperator(bpy.types.Operator):
            """Unity VFX Graph Six way emissive"""    #tooltip
            bl_idname = "render.unity_6way_emissive_prepare"
            bl_label = "Prepare emissive"
            bl_options = {'REGISTER', 'UNDO'}

            def execute(self, context):
                scene = context.scene
                unity6way = context.scene.unity6way

                tree = scene.node_tree
                layer_node = tree.nodes.new(type='CompositorNodeRLayers')

                output_node = _create_compositor_node_exr_output(tree)
                output_node.base_path = unity6way.temp_path
                output_node.file_slots.remove(output_node.inputs[0])
                output_node.file_slots.new(unity6way.emissive.filename)
                tree.links.new(layer_node.outputs["Image"], output_node.inputs[0])

                nodes = []
                nodes.append(layer_node)
                nodes.append(output_node)
                _restore_info["nodes"] = nodes
                return {'FINISHED'}
                
        class RestoreOperator(bpy.types.Operator):
            """Unity VFX Graph Six way render lighting"""    #tooltip
            bl_idname = "render.unity_6way_emissive_restore"
            bl_label = "Restore emissive"
            bl_options = {'REGISTER', 'UNDO'}

            def execute(self, context):
                _destroy_compositor_nodes(context.scene.node_tree, _restore_info["nodes"])
                return {'FINISHED'}     

        class ViewResultOperator(bpy.types.Operator):
            """Unity VFX Graph Six way render lighting"""    #tooltip
            bl_idname = "render.unity_6way_emissive_view"
            bl_label = "View last result"
            bl_options = {'REGISTER', 'UNDO'}

            def execute(self, context):
                scene = context.scene
                unity6way = scene.unity6way
                filename = _get_emissive_path(unity6way, _get_current_frame(scene))
                _show_image(filename, 'PREMUL')
                return {'FINISHED'}     

    class Compositing:

        class Properties(bpy.types.PropertyGroup):
            enabled: bpy.props.BoolProperty(
                name = "Enabled",
                default = True,
            )
            filename1 : bpy.props.StringProperty(
                name="Filename +",
                description = "Filename +",
                default="Positive",
            )            
            filename2 : bpy.props.StringProperty(
                name="Filename -",
                description = "Filename -",
                default="Negative",
            )            
            extra : bpy.props.EnumProperty(
                name='Extra Channel',
                description='Extra Channel',
                items={
                    ('NONE', 'None', "None (keep alpha)"),
                    ('EMISSIVE', 'Emissive', "Pack emissive information as gray-scale"),
                    ('CUSTOM', 'Custom', "Pack custom image data in extra channel"),
                },
                default='NONE'
            )
            custom_path : bpy.props.StringProperty(
                name="Custom image path",
                description = "Custom image path",
                default="",
                subtype='FILE_PATH',
            )
            premultiplied: bpy.props.BoolProperty(
                name = "Premultiplied alpha",
                description = "Premultiplied alpha",
                default = True,
            )
            lightmap_multiplier: bpy.props.FloatProperty(
                name = "Lightmap Multiplier",
                description = "Lightmap Multiplier",
                default = 1,
                min = 0,
            )
            extra_multiplier: bpy.props.FloatProperty(
                name = "Extra channel Multiplier",
                description = "Extra channel Multiplier",
                default = 1,
                min = 0,
            )
                
        class Panel(bpy.types.Panel):
            bl_idname = "VIEW3D_PT_unity_6way_compositing"
            bl_parent_id = "VIEW3D_PT_unity_6way"
            bl_label = "Compositing"
            bl_space_type = 'VIEW_3D'
            bl_region_type = 'UI'
            bl_options = {'DEFAULT_CLOSED'}

            def draw_header(self, context):
                self.layout.prop(context.scene.unity6way.compositing, "enabled", text="")

            def draw(self, context):
                scene = context.scene
                unity6way = scene.unity6way
                self.layout.prop(unity6way.compositing, "filename1")
                self.layout.prop(unity6way.compositing, "filename2")
                self.layout.label(text="Extra channel source:")
                self.layout.prop(unity6way.compositing, "extra", expand=True)
                row = self.layout.row()
                row.enabled = unity6way.compositing.extra == 'CUSTOM'
                row.prop(unity6way.compositing, "custom_path")
                self.layout.prop(unity6way.compositing, "premultiplied")
                self.layout.prop(unity6way.compositing, "lightmap_multiplier")
                row = self.layout.row()
                row.enabled = unity6way.compositing.extra != 'NONE'
                row.prop(unity6way.compositing, "extra_multiplier")
                #self.layout.operator(Unity6Way.Compositing.ViewResultOperator.bl_idname)
                render_operator = self.layout.operator(Unity6Way.RenderUndoOperator.bl_idname)
                render_operator.prepare_operator = "unity_6way_compositing_prepare"
                render_operator.restore_operator = "unity_6way_compositing_restore"
                
                row = self.layout.row()
                dest_paths = _get_compositing_paths(unity6way, _get_current_frame(scene))
                col = row.column()
                col.enabled = _file_exists(dest_paths[0])
                view_operator = col.operator(Unity6Way.Compositing.ViewResultOperator.bl_idname, text="View last +")
                view_operator.positive = True
                col = row.column()
                col.enabled = _file_exists(dest_paths[1])
                view_operator = col.operator(Unity6Way.Compositing.ViewResultOperator.bl_idname, text="View last -")
                view_operator.positive = False

        class PrepareOperator(bpy.types.Operator):
            """Unity VFX Graph Six way setup lighting"""    #tooltip
            bl_idname = "render.unity_6way_compositing_prepare"
            bl_label = "Prepare compositing"
            bl_options = {'REGISTER', 'UNDO'}

            def check_input_paths(self, scene):
                missing_paths = []

                unity6way = scene.unity6way
                
                if unity6way.compositing.extra == 'CUSTOM' and scene.frame_start == scene.frame_end:
                    _check_input_path(missing_paths, bpy.path.abspath(unity6way.compositing.custom_path))
                
                for frame in range(scene.frame_start, scene.frame_end + 1):
                    _check_input_path(missing_paths, _get_lightmaps_path(unity6way, frame))
                    if unity6way.compositing.extra == 'EMISSIVE':
                        _check_input_path(missing_paths, _get_emissive_path(unity6way, frame))
                    
                    if unity6way.compositing.extra == 'CUSTOM' and scene.frame_start < scene.frame_end:
                        _check_input_path(missing_paths,  bpy.path.abspath(unity6way.compositing.custom_path)) #TODO add frame number

                    if len(missing_paths) > 10:
                        break
                
                return missing_paths

            def execute(self, context):
                scene = context.scene
                unity6way = scene.unity6way

                nodes = []

                _restore_info["nodes"] = nodes   
                missing_paths = self.check_input_paths(scene)
                if missing_paths:
                    _report_missing_inputs(self, missing_paths)
                    return {'CANCELLED'}

                tree = scene.node_tree                
                lightmaps_path = _get_lightmaps_path(unity6way, scene.frame_start)
                input_node = _create_compositor_node_image_input(tree, _load_image(lightmaps_path), scene)
                nodes.append(input_node)
                                
                match unity6way.compositing.extra:
                    case 'NONE':
                        extra_node = input_node
                        extra_channel = "Alpha"
                    case 'EMISSIVE':
                        emissive_path = _get_emissive_path(unity6way, scene.frame_start)
                        extra_node = _create_compositor_node_image_input(tree, _load_image(emissive_path), scene)
                        extra_channel = 0
                    case 'CUSTOM':
                        extra_image = _load_image(bpy.path.abspath(unity6way.compositing.custom_path))
                        extra_node = _create_compositor_node_image_input(tree, extra_image, scene)
                        extra_channel = 0
                if extra_node != input_node:
                    nodes.append(extra_node)

                scale_node = tree.nodes.new(type='CompositorNodeScale')
                scale_node.space = 'RENDER_SIZE'
                scale_node.frame_method = 'STRETCH'
                nodes.append(scale_node)

                combiner_node = _create_node_group(tree, _6way_combiner_node_group_name, _add_6way_combiner_compositor_node_group)
                nodes.append(combiner_node)
                combiner_node.inputs["Lightmap Multiplier"].default_value = unity6way.compositing.lightmap_multiplier
                combiner_node.inputs["Extra Multiplier"].default_value = unity6way.compositing.extra_multiplier

                premultiply_value = 1 if unity6way.compositing.premultiplied else 0
                combiner_node.inputs["Premultiplied"].default_value = premultiply_value

                composite_node = tree.nodes.new(type='CompositorNodeComposite')
                nodes.append(composite_node)

                output_node = _create_compositor_node_exr_output(tree)
                nodes.append(output_node)
                output_node.base_path = unity6way.temp_path
                output_node.file_slots.remove(output_node.inputs[0])
                for slot_name in (unity6way.compositing.filename1, unity6way.compositing.filename2):
                    output_node.file_slots.new(slot_name)
                    output_node.file_slots[slot_name].use_node_format = True

                for dir_name in _light_direction_names:
                    tree.links.new(input_node.outputs[dir_name], combiner_node.inputs[dir_name])
                tree.links.new(input_node.outputs["Alpha"], combiner_node.inputs["Alpha"])
               
                tree.links.new(extra_node.outputs[extra_channel], scale_node.inputs["Image"])
                tree.links.new(scale_node.outputs["Image"], combiner_node.inputs["Extra"])
                
                tree.links.new(combiner_node.outputs["Positive"], output_node.inputs[0])
                tree.links.new(combiner_node.outputs["Negative"], output_node.inputs[1])

                tree.links.new(combiner_node.outputs["Positive"], composite_node.inputs[0])

                input_node.location = (-2 * _node_separation[0], 0)
                output_node.location = (_node_separation[0], 0)

                composite_node.location = (_node_separation[0], -2 * _node_separation[1])
                if extra_node != input_node:
                    extra_node.location = (-3*_node_separation[0], 0)
                    scale_node.location = (-2 * _node_separation[0], 0)
                
                scale_node.location = (-1 * _node_separation[0], 0)

                return {'FINISHED'}     

        class RestoreOperator(bpy.types.Operator):
            """Unity VFX Graph Six way render lighting"""    #tooltip
            bl_idname = "render.unity_6way_compositing_restore"
            bl_label = "Restore compositing"
            bl_options = {'REGISTER', 'UNDO'}

            def execute(self, context):
                _destroy_compositor_nodes(context.scene.node_tree, _restore_info["nodes"])
                return {'FINISHED'}     

        class ViewResultOperator(bpy.types.Operator):
            """Unity VFX Graph Six way render lighting"""    #tooltip
            bl_idname = "render.unity_6way_compositing_view"
            bl_label = "View last result"
            bl_options = {'REGISTER', 'UNDO'}

            positive: bpy.props.BoolProperty(default = False)

            def execute(self, context):
                scene = context.scene
                unity6way = scene.unity6way
                dest_paths = _get_compositing_paths(unity6way, _get_current_frame(scene))
                _show_image(dest_paths[0] if self.positive else dest_paths[1], 'PREMUL')
                return {'FINISHED'}

    class Flipbook:

        class Properties(bpy.types.PropertyGroup):
            enabled: bpy.props.BoolProperty(
                name = "Enabled",
                default = True,
            )
            use_temp: bpy.props.BoolProperty(
                name = "Use temp path",
                default = True,
            )
            dest_path : bpy.props.StringProperty(
                name="Export Path",
                description = "Export Path",
                default="",
                subtype='DIR_PATH')
            use_filename: bpy.props.BoolProperty(
                name = "Use Compositing filename",
                default = True,
            )
            filename1 : bpy.props.StringProperty(
                name="Filename +",
                description = "Image with lighting for positive axis",
                default="Positive",
            )            
            filename2 : bpy.props.StringProperty(
                name="Filename -",
                description = "Image with lighting for negative axis",
                default="Negative",
            )            
            dest_format : bpy.props.EnumProperty(
                name="Format",
                description="Format",
                items={
                    ('PNG', '.png', "PNG"),
                    ('TARGA', '.tga', "Targa"),
                    #('OPEN_EXR', '.exr', "Open EXR"),
                },
                default='PNG'
            )
            image_size: bpy.props.IntVectorProperty(
                name = "Image size",
                description = "Image size",
                default = (2048,2048),
                size = 2,
                min = 1,
            )
            tiling: bpy.props.IntVectorProperty(
                name = "Rows / Columns",
                description = "Rows and columns of the flipbook",
                default = (1,1),
                size = 2,
                min = 1,
            )

            frame_step: bpy.props.IntProperty(
                name = "Frame step",
                description = "Frame step",
                default = 1,
                min = 1,
            )
                        
        class Panel(bpy.types.Panel):
            bl_idname = "VIEW3D_PT_unity_6way_flipbook"
            bl_parent_id = "VIEW3D_PT_unity_6way"
            bl_label = "Flipbook"
            bl_space_type = 'VIEW_3D'
            bl_region_type = 'UI'
            bl_options = set()

            def draw_header(self, context):
                self.layout.prop(context.scene.unity6way.flipbook, "enabled", text="")

            def draw(self, context):
                unity6way = context.scene.unity6way
                self.layout.prop(unity6way.flipbook, "use_temp")
                row = self.layout.row()
                row.enabled = not unity6way.flipbook.use_temp
                row.prop(unity6way.flipbook, "dest_path")
                self.layout.prop(unity6way.flipbook, "use_filename")
                col = self.layout.column()
                col.enabled = not unity6way.flipbook.use_filename
                row = col.row()
                row.prop(unity6way.flipbook, "filename1")
                row = col.row()
                row.prop(unity6way.flipbook, "filename2")
                self.layout.prop(unity6way.flipbook, "dest_format", expand=True)
                self.layout.prop(unity6way.flipbook, "image_size")
                self.layout.prop(unity6way.flipbook, "tiling")
                row = self.layout.row()
                row.prop(unity6way.flipbook, "frame_step")
                self.layout.operator(Unity6Way.Flipbook.ExportOperator.bl_idname)

                row = self.layout.row()
                output_paths = _get_export_paths(unity6way)
                col = row.column()
                col.enabled = _file_exists(output_paths[0])
                view_operator = col.operator(Unity6Way.Flipbook.ViewResultOperator.bl_idname, text="View last +")
                view_operator.positive = True
                col = row.column()
                col.enabled = _file_exists(output_paths[1])
                view_operator = col.operator(Unity6Way.Flipbook.ViewResultOperator.bl_idname, text="View last -")
                view_operator.positive = False
                

        class ExportOperator(bpy.types.Operator):
            """Unity VFX Graph Six way render lighting"""    #tooltip
            bl_idname = "render.unity_6way_flipbook_export"
            bl_label = "Export Flipbook"
            bl_options = {'REGISTER', 'UNDO'}

            def check_input_paths(self, unity6way, frame_start, frame_end):
                missing_paths = []
                
                for frame in range(frame_start, frame_end + 1):
                    input_paths = _get_compositing_paths(unity6way, frame)
                    _check_input_path(missing_paths, input_paths[0])
                    _check_input_path(missing_paths, input_paths[1])

                    if len(missing_paths) > 8:
                        break
                
                return missing_paths

            def execute(self, context):
                scene = context.scene
                unity6way = scene.unity6way

                frame_start, frame_end = _get_frames_range(scene)

                missing_paths = self.check_input_paths(unity6way, frame_start, frame_end)
                if missing_paths:
                    _report_missing_inputs(self, missing_paths)
                    return {'CANCELLED'}

                tiling = unity6way.flipbook.tiling

                flipbook_size = unity6way.flipbook.image_size
                flipbook_row = flipbook_size[0] * 4
                flipbook_total = flipbook_row * flipbook_size[1]

                tile_width = flipbook_size[0] // tiling[0]
                tile_height = flipbook_size[1] // tiling[1]
                tile_row = tile_width * 4

                dst_pixels = [0] * (flipbook_total * 2)

                wm = context.window_manager
                wm.progress_begin(frame_start, frame_end + 1)

                for frame in range(frame_start, frame_end + 1):
                    frame_index = frame - frame_start
                    tile_x = frame_index % tiling[0]
                    tile_y = frame_index // tiling[0]
                    if tile_y < tiling[1]:
                        tile_y = tiling[1] - tile_y - 1
                        img_index = min(frame_end, max(1, (frame * unity6way.flipbook.frame_step) - 1))
                        input_paths = _get_compositing_paths(unity6way, img_index)
                        for i in range(2):
                            src_image = _load_image(input_paths[i])
                            src_image.scale(tile_width, tile_height)
                            src_pixels = list(src_image.pixels)

                            src_offset = 0
                            dst_offset = flipbook_total * i + tile_x * tile_row + tile_y * tile_height * flipbook_row
                            for y in range(tile_height):
                                dst_pixels[dst_offset:dst_offset+tile_row] = src_pixels[src_offset:src_offset+tile_row]
                                src_offset += tile_row
                                dst_offset += flipbook_row
                            
                            bpy.data.images.remove(src_image)                            
                    wm.progress_update(frame)

                wm.progress_end()

                output_paths = _get_export_paths(unity6way)
                for i in range(2):
                    
                    output_filename = bpy.path.basename(output_paths[i])
                    output_image = bpy.data.images.get(output_filename)
                    if output_image != None:
                        bpy.data.images.remove(output_image)
                    output_image = bpy.data.images.new(output_filename, width=flipbook_size[0], height=flipbook_size[1], alpha=True)
                    output_image.alpha_mode = 'CHANNEL_PACKED'
                    output_image.filepath_raw = output_paths[i]
                    output_image.file_format = unity6way.flipbook.dest_format
                    output_image.pixels = dst_pixels[flipbook_total*i:flipbook_total*(i+1)]

                    #workaround to save files to different formats (save not working properly)
                    #read scene settings
                    settings = scene.render.image_settings
                    current_format = settings.file_format
                    current_mode = settings.color_mode
                    current_depth = settings.color_depth
                    
                    #set image scene settings
                    settings.file_format = unity6way.flipbook.dest_format
                    settings.color_mode = 'RGBA'
                    settings.color_depth = '16' if unity6way.flipbook.dest_format == 'OPEN_EXR' else '8'                    

                    output_image.save_render(filepath=output_paths[i]) 

                    #restore scene settings
                    settings.file_format = current_format
                    settings.color_mode = current_mode
                    settings.color_depth = current_depth

                _show_image(output_paths[0], 'CHANNEL_PACKED')
                                
                return {'FINISHED'}     

        class ViewResultOperator(bpy.types.Operator):
            """Unity VFX Graph Six way render lighting"""    #tooltip
            bl_idname = "render.unity_6way_export_view"
            bl_label = "View last result"
            bl_options = {'REGISTER', 'UNDO'}

            positive: bpy.props.BoolProperty(default = False)

            def execute(self, context):
                scene = context.scene
                unity6way = scene.unity6way
                output_paths = _get_export_paths(unity6way)
                _show_image(output_paths[0] if self.positive else output_paths[1], 'CHANNEL_PACKED')
                return {'FINISHED'}


    class RenderUndoOperator(bpy.types.Operator):
        """Unity VFX Graph Six way render operator"""    #tooltip
        bl_idname = "render.unity_6way_render"
        bl_label = "Render"
        bl_options = {'REGISTER', 'UNDO'}

        prepare_operator: bpy.props.StringProperty()
        restore_operator: bpy.props.StringProperty()

        
        _restore_start_frame = 0
        _restore_end_frame = 0
        _restore_scene_info = {}
        _restore_world_info = {}
        _restore_nodes = []
        _restore_lights = []
        _restore_lock_interface = False        

        _timer = None

        def _prepare(self, context):
            scene = context.scene
            
            scene.unity6way.is_cancelled = False
            scene.unity6way.is_locked = True
            self._restore_lock_interface = scene.render.use_lock_interface
            scene.render.use_lock_interface = True

            self._prepare_scene(scene)
            self._prepare_world(scene.world)
            self._prepare_frames_range(scene)
            self._disable_existing_nodes(scene.node_tree)
            self._disable_existing_lights()

            _restore_info = {}
            getattr(bpy.ops.render, self.prepare_operator)()

            bpy.app.handlers.render_init.append(_on_render_init)
            bpy.app.handlers.render_cancel.append(_on_render_cancel)
            bpy.app.handlers.render_complete.append(_on_render_complete)
            
            # should be here, but it needs to be started later or it gets removed
            #self._timer = context.window_manager.event_timer_add(0.1, window=context.window)

        def _restore(self, context):
            scene = context.scene
            
            context.window_manager.event_timer_remove(self._timer)

            bpy.app.handlers.render_complete.remove(_on_render_complete)
            bpy.app.handlers.render_cancel.remove(_on_render_cancel)
            bpy.app.handlers.render_init.remove(_on_render_init)

            getattr(bpy.ops.render, self.restore_operator)()
            _restore_info = {}

            self._restore_existing_lights()
            self._restore_existing_nodes(scene.node_tree)
            self._restore_frames_range(scene)
            self._restore_world(scene.world)
            self._restore_scene(scene)

            scene.render.use_lock_interface = self._restore_lock_interface
            scene.unity6way.is_locked = False

        def _prepare_frames_range(self, scene):
            self._restore_frame_start = scene.frame_start
            self._restore_frame_end = scene.frame_end
            frame_start, frame_end = _get_frames_range(scene)
            scene.frame_start = frame_start
            scene.frame_end = frame_end

        def _restore_frames_range(self, scene):
            scene.frame_start = self._restore_frame_start
            scene.frame_end = self._restore_frame_end

        def _prepare_scene(self, scene):
            self._restore_scene_info = {}
            self._restore_scene_info["use_nodes"] = scene.use_nodes
            scene.use_nodes = True
            self._restore_scene_info["film_transparent"] = scene.render.film_transparent
            scene.render.film_transparent = True
            self._restore_scene_info["exposure"] = scene.view_settings.exposure
            scene.view_settings.exposure = 0
            self._restore_scene_info["gamma"] = scene.view_settings.gamma
            scene.view_settings.gamma = 2.2
            self._restore_scene_info["view_transform"] = scene.view_settings.view_transform
            scene.view_settings.view_transform = 'Raw'

        def _restore_scene(self, scene):
            scene.use_nodes = self._restore_scene_info["use_nodes"]
            scene.render.film_transparent = self._restore_scene_info["film_transparent"]
            scene.view_settings.exposure = self._restore_scene_info["exposure"]
            scene.view_settings.gamma = self._restore_scene_info["gamma"]
            scene.view_settings.view_transform = self._restore_scene_info["view_transform"]
            self._restore_scene_info = {}

        def _prepare_world(self, world):
            self._restore_world_info["use_nodes"] = world.use_nodes
            world.use_nodes = False
            self._restore_world_info["bg_color"] = world.color
            world.color = [0, 0, 0]

        def _restore_world(self, world):
            world.use_nodes = self._restore_world_info["use_nodes"]
            world.color = self._restore_world_info["bg_color"]

        def _disable_existing_nodes(self, tree):
            self._restore_nodes = []
            for node in tree.nodes:
                if not node.mute:
                    self._restore_nodes.append(node)
                    node.mute = True

        def _restore_existing_nodes(self, tree):
            for node in self._restore_nodes:
                node.mute = False

        def _disable_existing_lights(self):
            self._restore_lights = []
            for object in bpy.data.objects:
                if object.type == 'LIGHT':
                    light_object = object
                    if not light_object.hide_render:
                        self._restore_lights.append(light_object)
                        light_object.hide_render = True

        def _restore_existing_lights(self):
            for light_object in self._restore_lights:
                light_object.hide_render = False

        def modal(self, context, event):
            if context.scene.unity6way.is_rendering:
                return {'RUNNING_MODAL'}
            self._restore(context)
            return {'FINISHED'}

        def cancel(self, context):
            context.scene.unity6way.is_cancelled = True
            self._restore(context)
 
        def execute(self, context):
            scene = context.scene
            unity6way = scene.unity6way
            
            self._prepare(context)
            _frame_start, _frame_end = _get_frames_range(scene)

            bpy.ops.render.render('INVOKE_DEFAULT', animation=True)

            self._timer = context.window_manager.event_timer_add(0.1, window=context.window)
            context.window_manager.modal_handler_add(self)
            return {'RUNNING_MODAL'}

    class RenderAllOperator(bpy.types.Operator):
        """Unity VFX Graph Six way render lighting"""    #tooltip
        bl_idname = "render.unity_6way_render_all"
        bl_label = "Render all"
        bl_options = {'REGISTER', 'UNDO'}

        _stages = ['LIGHTMAPS', 'EMISSIVE', 'COMPOSITING', 'FLIPBOOK']
        _stage_index = 0

        _timer = None

        def _run_next_stage(self, context):
            stage = self._stages[self._stage_index] if self._stage_index < len(self._stages) else ''
            match stage:
                case 'LIGHTMAPS':
                    bpy.ops.render.unity_6way_render(prepare_operator = "unity_6way_lightmap_prepare", restore_operator = "unity_6way_lightmap_restore")
                    return {'RUNNING_MODAL'}
                case 'EMISSIVE':
                    bpy.ops.render.unity_6way_render(prepare_operator = "unity_6way_emissive_prepare", restore_operator = "unity_6way_emissive_restore")
                    return {'RUNNING_MODAL'}
                case 'COMPOSITING':
                    bpy.ops.render.unity_6way_render(prepare_operator = "unity_6way_compositing_prepare", restore_operator = "unity_6way_compositing_restore")
                    return {'RUNNING_MODAL'}
                case 'FLIPBOOK':
                    bpy.ops.render.unity_6way_flipbook_export()
                    return {'RUNNING_MODAL'}

            context.window_manager.event_timer_remove(self._timer)

            return {'FINISHED'}

        def modal(self, context, event):
            if context.scene.unity6way.is_locked:
                return {'RUNNING_MODAL'}

            if context.scene.unity6way.is_cancelled:
                return {'CANCELLED'}

            self._stage_index += 1
            return self._run_next_stage(context)

        def execute(self, context):
            unity6way = context.scene.unity6way

            self._stages = []
            if unity6way.lightmaps.enabled:
                self._stages.append('LIGHTMAPS')

            if unity6way.emissive.enabled:
                self._stages.append('EMISSIVE')

            if unity6way.compositing.enabled:
                self._stages.append('COMPOSITING')

            if unity6way.flipbook.enabled:
                self._stages.append('FLIPBOOK')

            self._timer = context.window_manager.event_timer_add(0.1, window=context.window)

            context.window_manager.modal_handler_add(self)

            self._stage_index = 0
            return self._run_next_stage(context)

class Unity6WayProperties(bpy.types.PropertyGroup):
    temp_path : bpy.props.StringProperty(
        name="Temp Path",
        description = "Temp Path",
        default="/tmp\\",
        subtype='DIR_PATH',
        )
    frames : bpy.props.EnumProperty(
        name='Frames',
        description='Frames',
        items={
            ('CURRENT', 'Current settings', "Current frame range configuration", 0),
            ('FRAME', 'Frame', "Single Frame", 1),
            ('RANGE', 'Range', "Frame Range", 2),
        },
        default='CURRENT',
    )
    frame_start: bpy.props.IntProperty(
        name = "Frame start",
        description = "Frame start",
        default = 1,
        min = 1,
    )
    frame_end: bpy.props.IntProperty(
        name = "Frame end",
        description = "Frame end",
        default = 250,
        min = 1,
    )
    lightmaps : bpy.props.PointerProperty(type=Unity6Way.Lightmaps.Properties)
    emissive : bpy.props.PointerProperty(type=Unity6Way.Emissive.Properties)
    compositing : bpy.props.PointerProperty(type=Unity6Way.Compositing.Properties)
    flipbook : bpy.props.PointerProperty(type=Unity6Way.Flipbook.Properties)
    is_rendering: bpy.props.BoolProperty(default = False)
    is_locked: bpy.props.BoolProperty(default = False)
    is_cancelled: bpy.props.BoolProperty(default = False)

classes = (
    Unity6Way.Panel,
    Unity6Way.Lightmaps.Panel,
    Unity6Way.Emissive.Panel,
    Unity6Way.Compositing.Panel,
    Unity6Way.Flipbook.Panel,

    Unity6Way.Lightmaps.PrepareOperator,
    Unity6Way.Lightmaps.RestoreOperator,
    Unity6Way.Lightmaps.ViewResultOperator,

    Unity6Way.Emissive.PrepareOperator,
    Unity6Way.Emissive.RestoreOperator,
    Unity6Way.Emissive.ViewResultOperator,

    Unity6Way.Compositing.PrepareOperator,
    Unity6Way.Compositing.RestoreOperator,
    Unity6Way.Compositing.ViewResultOperator,

    Unity6Way.Flipbook.ExportOperator,
    Unity6Way.Flipbook.ViewResultOperator,

    Unity6Way.RenderUndoOperator,
    Unity6Way.RenderAllOperator,

    Unity6Way.Lightmaps.Properties,
    Unity6Way.Emissive.Properties,
    Unity6Way.Compositing.Properties,
    Unity6Way.Flipbook.Properties,
    Unity6WayProperties,
)

def register():
    for cls in classes:
        bpy.utils.register_class(cls)
    bpy.types.Scene.unity6way = bpy.props.PointerProperty(type=Unity6WayProperties)
    

def unregister():
    del bpy.types.Scene.unity6way
    for cls in classes:
        bpy.utils.unregister_class(cls)
