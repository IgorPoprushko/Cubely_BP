using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.Scripting.APIUpdating;

[System.Serializable]
[MovedFrom(true, null, null, "SocketDefinition")]
public class BlockSlotDefinition
{
    public string name;
    [FormerlySerializedAs("repativePosition")]
    public Vector3 localPosition;
    public Vector3 localEulerAngles;
}

[CreateAssetMenu(menuName = "Code Tree/Block Definition")]
[MovedFrom(true, null, null, "CodeBlockScriptableObject")]
public class BlockDefinition : ScriptableObject
{
    [Header("Identity")]
    public string displayName;
    public Sprite icon;
    public Color color = Color.white;
    [TextArea] public string codeText;

    [Header("Slots (in execution order)")]
    [FormerlySerializedAs("sockets")]
    public BlockSlotDefinition[] slots = System.Array.Empty<BlockSlotDefinition>();

}
