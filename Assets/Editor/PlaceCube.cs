using UnityEditor;
using UnityEngine;

public static class PlaceCube
{
    [MenuItem("RED MET/Place Cube at Origin")]
    public static void PlaceCubeAtOrigin()
    {
        var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.name = "Cube";
        cube.transform.position = Vector3.zero;
        Undo.RegisterCreatedObjectUndo(cube, "Place Cube at Origin");
        Selection.activeGameObject = cube;
    }
}
