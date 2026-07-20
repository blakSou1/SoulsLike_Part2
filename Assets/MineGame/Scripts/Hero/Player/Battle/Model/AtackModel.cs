using UnityEngine;

[CreateAssetMenu(menuName = "Battle/AtackModel")]
public class AtackModel : ScriptableObject
{
    [Header("")]
    public ItemActionContainerModel inputStatsAction;

}
[System.Serializable]
public class ItemActionContainerModel
{
    [Header("Название воспроизводимой анимации")]
    public string animName;

    [Header("Префаб с эфектом удара если нужен")]
    public GameObject prefabSlashFX;

    [Header("блокирует ли эта анимация управление игрока? да-нет")]
    public bool isInteracting = true;

    public MotionWarpSettings warpSettings;
}