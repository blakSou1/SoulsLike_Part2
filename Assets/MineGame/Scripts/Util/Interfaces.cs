using UnityEngine;

/// <summary>
/// прикручивается к обектам на которых можно заблокировать камеру.
/// </summary>
public interface ILockable
{
    /// <summary>
    /// возвращает фолс если нужно сбросить блокировку камеры например при смерти врага
    /// </summary>
    /// <returns>булевая переменная фолсе-сбросить блокировку</returns>
    public bool IsAlive();

    /// <summary>
    /// возвращает трансформ за которым будет заблокирован обзор камеры
    /// </summary>
    /// <param name="from">TODO</param>
    /// <returns>трансформ - цель</returns>
    public Transform GetLockOnTarget(Transform from);
}