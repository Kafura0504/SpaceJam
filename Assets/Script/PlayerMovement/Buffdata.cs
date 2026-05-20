using UnityEngine;

    public enum BuffType
{
    HP,
    Damage,
    FireRate,
    Magazine,
    BulletVelocity,
    IframeDuration
}
[CreateAssetMenu(fileName ="New Buff", menuName ="MyAsset/BuffData")]
public class BuffData : ScriptableObject
{

    public string buffName;
    public string description;
    public Sprite Image;

    public BuffType type;

    public float value;
}
