import bpy
import math
import mathutils

__base_path = "" # ADD YOUR PATH HERE

__6way_filename = "temp"
__emissive_filename = "emissive"

__energy = math.pi
__angle = 0 #math.pi * 0.5
__premultiply = False


__name_prefix = "unity_6way"

__light_direction_names = ("Right", "Left", "Bottom", "Top", "Front", "Back")

__rgba_combiner_node_group_name = "UnityRGBACombinerGroup"
__6way_combiner_node_group_name = "Unity6wayCombinerGroup"

__node_separation = (200, 100)

def remove_compositor_node_group(group_name):
    if bpy.data.node_groups.__contains__(group_name):
        bpy.data.node_groups.remove(bpy.data.node_groups[group_name])

def add_compositor_node_group(group_name):
    return bpy.data.node_groups.new(group_name, 'CompositorNodeTree')

def add_rgba_combiner_compositor_node_group():
    tree = add_compositor_node_group(__rgba_combiner_node_group_name)
    
    group_inputs = tree.nodes.new('NodeGroupInput')
    group_inputs.location = (-2 * __node_separation[0], 0)
    tree.inputs.new('NodeSocketColor',"R")
    tree.inputs.new('NodeSocketColor',"G")
    tree.inputs.new('NodeSocketColor',"B")
    tree.inputs.new('NodeSocketColor',"A")
    tree.inputs.new('NodeSocketFloatFactor',"Premultiply")
    
    group_outputs = tree.nodes.new('NodeGroupOutput')
    group_outputs.location = (2 * __node_separation[0], 0)
    tree.outputs.new('NodeSocketColor',"Image")
    
    rgba_node = tree.nodes.new(type='CompositorNodeCombRGBA')
    
    multiply_node = tree.nodes.new(type='CompositorNodeMixRGB')
    multiply_node.location = (__node_separation[0], 0)
    multiply_node.use_clamp = True
    multiply_node.blend_type = 'MULTIPLY'
    multiply_node.inputs["Fac"].default_value = 0
    
    location_y = 1.5 * __node_separation[1]
    for rgba_slot in rgba_node.inputs:
        bw_node = tree.nodes.new(type='CompositorNodeRGBToBW')
        bw_node.location = (-__node_separation[0], location_y)
        tree.links.new(bw_node.outputs["Val"], rgba_slot)
        tree.links.new(group_inputs.outputs[rgba_slot.name], bw_node.inputs["Image"])
        location_y -= __node_separation[1]
        
    tree.links.new(rgba_node.outputs["Image"], multiply_node.inputs[1])
    tree.links.new(bw_node.outputs["Val"], multiply_node.inputs[2])
    tree.links.new(multiply_node.outputs["Image"], group_outputs.inputs["Image"])
    tree.links.new(group_inputs.outputs["Premultiply"], multiply_node.inputs["Fac"])

def add_6way_combiner_compositor_node_group():
    tree = add_compositor_node_group(__6way_combiner_node_group_name)
    
    group_inputs = tree.nodes.new('NodeGroupInput')
    group_inputs.location = (-1.5 * __node_separation[0], 0)
    for slot_name in __light_direction_names:
        tree.inputs.new('NodeSocketColor',slot_name)
    tree.inputs.new('NodeSocketColor',"Alpha")
    tree.inputs.new('NodeSocketColor',"Extra")
    tree.inputs.new('NodeSocketFloatFactor',"Premultiply")

    group_outputs = tree.nodes.new('NodeGroupOutput')
    group_outputs.location = (2 * __node_separation[0], 0)
    tree.outputs.new('NodeSocketColor',"Positive")
    tree.outputs.new('NodeSocketColor',"Negative")

    positive_node = tree.nodes.new(type='CompositorNodeGroup')
    positive_node.node_tree = bpy.data.node_groups[__rgba_combiner_node_group_name]
    positive_node.location = (0, 2 * __node_separation[1])
    
    negative_node = tree.nodes.new(type='CompositorNodeGroup')
    negative_node.node_tree = positive_node.node_tree
    negative_node.location = (0, -2 * __node_separation[1])

    additional_node = tree.nodes.new(type='CompositorNodeSetAlpha')
    additional_node.mode = 'REPLACE_ALPHA'
    additional_node.location = (__node_separation[0], -0.5 * __node_separation[1])

    tree.links.new(group_inputs.outputs["Right" ], positive_node.inputs["R"])
    tree.links.new(group_inputs.outputs["Left"  ], negative_node.inputs["R"])
    tree.links.new(group_inputs.outputs["Bottom"], positive_node.inputs["G"])
    tree.links.new(group_inputs.outputs["Top"   ], negative_node.inputs["G"])
    tree.links.new(group_inputs.outputs["Front" ], positive_node.inputs["B"])
    tree.links.new(group_inputs.outputs["Back"  ], negative_node.inputs["B"])
    tree.links.new(group_inputs.outputs["Alpha" ], positive_node.inputs["A"])
    tree.links.new(group_inputs.outputs["Alpha" ], negative_node.inputs["A"])
    tree.links.new(group_inputs.outputs["Premultiply"], positive_node.inputs["Premultiply"])
    tree.links.new(group_inputs.outputs["Premultiply"], negative_node.inputs["Premultiply"])

    tree.links.new(negative_node.outputs["Image"], additional_node.inputs["Image"])
    tree.links.new(group_inputs.outputs["Extra"], additional_node.inputs["Alpha"])

    tree.links.new(positive_node.outputs["Image"], group_outputs.inputs["Positive"])
    tree.links.new(additional_node.outputs["Image"], group_outputs.inputs["Negative"])


def create_lights(parent_collection, camera):
    light_data = bpy.data.lights.new(name=__name_prefix+"_light", type='SUN')
    light_data.energy = __energy
    light_data.angle = __angle

    light_data.specular_factor = 0

    lights = {}
    for dir_name in __light_direction_names:
        collection = bpy.data.collections.new(__name_prefix+dir_name)
        parent_collection.children.link(collection)
        light = bpy.data.objects.new(name=__name_prefix+dir_name, object_data=light_data)
        light.rotation_mode = 'QUATERNION'
        collection.objects.link(light)
        lights[dir_name] = light

    if camera.rotation_mode == 'QUATERNION':
        camera_rotation = camera.rotation_quaternion
    elif camera.rotation_mode == 'AXIS_ANGLE':
        camera_rotation = camera.rotation_axis_angle.to_quaternion()
    else:
        camera_rotation = camera.rotation_euler.to_quaternion()

    pi = math.pi
    pi_2 = pi * 0.5

    lights["Front" ].rotation_quaternion = camera_rotation 
    lights["Back"  ].rotation_quaternion = camera_rotation @ mathutils.Quaternion([0, 1, 0], pi)
    lights["Left"  ].rotation_quaternion = camera_rotation @ mathutils.Quaternion([0, 1, 0], -pi_2) 
    lights["Right" ].rotation_quaternion = camera_rotation @ mathutils.Quaternion([0, 1, 0], pi_2) 
    lights["Top"   ].rotation_quaternion = camera_rotation @ mathutils.Quaternion([1, 0, 0], -pi_2) 
    lights["Bottom"].rotation_quaternion = camera_rotation @ mathutils.Quaternion([1, 0, 0], pi_2) 

    return lights

def destroy_lights(lights):
    light_data = lights[__light_direction_names[0]].data
    for light in lights.values():
        bpy.data.collections.remove(bpy.data.collections[light.name])
    bpy.data.lights.remove(light_data)


def create_layers():
    view_layers = {}
    for dir_name in __light_direction_names:
        layer = bpy.context.scene.view_layers.new(__name_prefix+dir_name)
        #disable other light collections
        for other_dir_name in __light_direction_names:
            layer.layer_collection.children[__name_prefix+other_dir_name].exclude = True
        #enable matching light collection
        layer.layer_collection.children[layer.name].exclude = False
        #add to layers
        view_layers[dir_name] = layer
    return view_layers

def destroy_layers(view_layers):
    for layer in view_layers.values():
        bpy.context.scene.view_layers.remove(layer)


def create_compositor_node_render_layers(tree):
    layer_nodes = {}
    for dir_name in __light_direction_names:
        layer_node = tree.nodes.new(type='CompositorNodeRLayers')
        layer_node.layer = __name_prefix+dir_name
        layer_nodes[dir_name] = layer_node
    return layer_nodes

def create_compositor_node_render_layer(tree):
    layer_node = tree.nodes.new(type='CompositorNodeRLayers')
    return layer_node

def create_compositor_node_exr_input(tree, filename):
    input_node = tree.nodes.new(type='CompositorNodeImage')
    input_node.image = bpy.data.images.load(__base_path+"\\"+filename+"0001.exr", check_existing=False)    
    return input_node

def create_compositor_node_exr_output(tree):
    output_node = tree.nodes.new(type='CompositorNodeOutputFile')
    output_node.format.file_format = 'OPEN_EXR_MULTILAYER'
    output_node.format.color_mode = 'RGBA'
    output_node.format.color_depth = '32'
    output_node.format.quality = 100
    
    return output_node

def create_compositor_node_6way_combiner(tree):
    lighting_node = tree.nodes.new(type='CompositorNodeGroup')
    lighting_node.node_tree = bpy.data.node_groups[__6way_combiner_node_group_name]
    return lighting_node

def create_compositor_node_final_output(tree):
    output_node = tree.nodes.new(type='CompositorNodeOutputFile')
    output_node.format.file_format = 'TARGA'
    output_node.format.color_mode = 'RGBA'
    output_node.format.color_depth = '8'
    output_node.format.quality = 100
    output_node.format.compression = 0
    output_node.format.linear_colorspace_settings.name = 'sRGB'
    output_node.file_slots.remove(output_node.inputs[0])

    for slot_name in ("Positive", "Negative"):
        output_node.file_slots.new(slot_name)
        output_node.file_slots[slot_name].use_node_format = True

    return output_node


#from here this should be addon


def create_compositor_nodes_emissive_render(tree):
    layer_node = create_compositor_node_render_layer(tree)

    output_node = create_compositor_node_exr_output(tree)
    output_node.base_path = __base_path+"\\"+__emissive_filename

    tree.links.new(layer_node.outputs["Image"], output_node.inputs[0])

    nodes = []
    nodes.append(layer_node)
    nodes.append(output_node)
    return nodes

def create_compositor_nodes_6way_render(tree):
    layer_nodes = create_compositor_node_render_layers(tree)

    output_node = create_compositor_node_exr_output(tree)
    output_node.base_path = __base_path+"\\"+__6way_filename

    output_node.file_slots.remove(output_node.inputs[0])

    for dir_name in __light_direction_names:
        output_node.file_slots.new(dir_name)
        tree.links.new(layer_nodes[dir_name].outputs["Image"], output_node.inputs[dir_name])

    output_node.file_slots.new("Alpha")
    tree.links.new(layer_nodes[__light_direction_names[0]].outputs["Alpha"], output_node.inputs["Alpha"])

    layer_nodes["Left"  ].location = (-4 * __node_separation[0], 4 * __node_separation[1])
    layer_nodes["Right" ].location = (-2 * __node_separation[0], 4 * __node_separation[1])
    layer_nodes["Bottom"].location = (-4 * __node_separation[0], 0)
    layer_nodes["Top"   ].location = (-2 * __node_separation[0], 0)
    layer_nodes["Front" ].location = (-4 * __node_separation[0], -4 * __node_separation[1])
    layer_nodes["Back"  ].location = (-2 * __node_separation[0], -4 * __node_separation[1])

    nodes = []
    for layer_node in layer_nodes.values():
        nodes.append(layer_node)
    nodes.append(output_node)
    return nodes

def create_compositor_nodes_final_compositing(tree):
    input_node = create_compositor_node_exr_input(tree, __6way_filename)
    
    extra_filename = __emissive_filename
    extra_node = create_compositor_node_exr_input(tree, extra_filename)

    combiner_node = create_compositor_node_6way_combiner(tree)

    premultiply_value = 0
    #Currently, premultiplied as default render, no need to multiply, this has to be changed
    #if __premultiply:
    #    premultiply_value = 1
    combiner_node.inputs["Premultiply"].default_value = premultiply_value

    output_node = create_compositor_node_final_output(tree)
    output_node.base_path = __base_path

    for dir_name in __light_direction_names:
        tree.links.new(input_node.outputs[dir_name], combiner_node.inputs[dir_name])
    tree.links.new(input_node.outputs["Alpha"], combiner_node.inputs["Alpha"])
    tree.links.new(extra_node.outputs[0], combiner_node.inputs["Extra"])

    tree.links.new(combiner_node.outputs["Positive"], output_node.inputs["Positive"])
    tree.links.new(combiner_node.outputs["Negative"], output_node.inputs["Negative"])

    input_node.location = (-__node_separation[0], 0)
    output_node.location = (__node_separation[0], 0)

    nodes = []
    nodes.append(input_node)
    nodes.append(extra_node)
    nodes.append(combiner_node)
    nodes.append(output_node)
    return nodes

def destroy_compositor_nodes(tree, nodes):
    for node in nodes:
        tree.nodes.remove(node)


def disable_existing_lights():
    light_objects = []
    for object in bpy.data.objects:
        if object.type == 'LIGHT':
            light_object = object
            if not light_object.hide_render:
                light_objects.append(light_object)
                light_object.hide_render = True
    return light_objects

def restore_existing_lights(light_objects):
    for light_object in light_objects:
        light_object.hide_render = False

def disable_emissive_materials():
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
    return restore_emissive_infos

def restore_emissive_materials(restore_emissive_infos):
    for restore_emissive_info in restore_emissive_infos:
        material = restore_emissive_info[0]
        zero_value_node = restore_emissive_info[1]
        socket_pairs = restore_emissive_info[2]
        material.node_tree.nodes.remove(zero_value_node)
        for socket_pair in socket_pairs:
            material.node_tree.links.new(socket_pair[0], socket_pair[1])

def prepare_scene():
    restore_settings = {}
    
    scene = bpy.context.scene
    restore_settings["SceneNodes"] = scene.use_nodes
    scene.use_nodes = True
    restore_settings["Transparent"] = scene.render.film_transparent
    scene.render.film_transparent = True
    restore_settings["Exposure"] = scene.view_settings.exposure
    scene.view_settings.exposure = 0
    restore_settings["Gamma"] = scene.view_settings.gamma
    scene.view_settings.gamma = 2.2
    restore_settings["ViewTransform"] = scene.view_settings.view_transform
    scene.view_settings.view_transform = 'Raw'

    world = scene.world
    restore_settings["WorldNodes"] = world.use_nodes
    world.use_nodes = False
    restore_settings["WorldColor"] = world.color
    world.color = [0, 0, 0]

    restore_settings["Lights"] = disable_existing_lights()
    
    return restore_settings

def restore_scene(restore_settings):
    
    scene = bpy.context.scene
    scene.use_nodes = restore_settings["SceneNodes"]
    scene.render.film_transparent = restore_settings["Transparent"]
    scene.view_settings.exposure = restore_settings["Exposure"]
    scene.view_settings.gamma = restore_settings["Gamma"]
    scene.view_settings.view_transform = restore_settings["ViewTransform"]

    world = scene.world
    world.use_nodes = restore_settings["WorldNodes"]
    world.color = restore_settings["WorldColor"]

    restore_existing_lights(restore_settings["Lights"])


def render_emissive():
    scene = bpy.context.scene

    compositor_nodes = create_compositor_nodes_emissive_render(scene.node_tree)

    bpy.ops.render.render()

    destroy_compositor_nodes(scene.node_tree, compositor_nodes)

def render_6way():
    scene = bpy.context.scene

    lights = create_lights(scene.collection, scene.camera)
    view_layers = create_layers()
    compositor_nodes = create_compositor_nodes_6way_render(scene.node_tree)

    restore_emissive_infos = disable_emissive_materials()

    bpy.ops.render.render()

    restore_emissive_materials(restore_emissive_infos)

    destroy_compositor_nodes(scene.node_tree, compositor_nodes)
    destroy_layers(view_layers)
    destroy_lights(lights)   

def compose_final_image():
    scene = bpy.context.scene

    compositor_nodes = create_compositor_nodes_final_compositing(scene.node_tree)

    bpy.ops.render.render()

    bpy.data.images.remove(compositor_nodes[0].image) #6way
    bpy.data.images.remove(compositor_nodes[1].image) #extra

    destroy_compositor_nodes(scene.node_tree, compositor_nodes)



remove_compositor_node_group(__rgba_combiner_node_group_name)
remove_compositor_node_group(__6way_combiner_node_group_name)
add_rgba_combiner_compositor_node_group()
add_6way_combiner_compositor_node_group()



restore_settings = prepare_scene()

render_emissive()
render_6way()
compose_final_image()

restore_scene(restore_settings)
