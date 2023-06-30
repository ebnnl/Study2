import bpy

def export_all_fbx(exportFolder):
    objects = bpy.data.objects
    for object in objects:
        bpy.ops.object.select_all(action='DESELECT')
        object.select = True
        exportName = exportFolder + object.name + '.fbx'
        bpy.ops.export_scene.fbx(filepath=exportName, use_selection=True)
