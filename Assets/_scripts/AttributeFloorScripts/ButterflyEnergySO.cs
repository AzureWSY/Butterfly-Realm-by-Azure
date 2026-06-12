using UnityEngine;
using System.Collections;

[CreateAssetMenu(menuName = "Butterfly Realm/Effects/ButterflyEnergy")]
public class ButterflyEnergyFloorSO : FloorEffectSO
{
    [Header("蝴蝶能量配置")]
    public int energyAmount = 20;

    [Header("能量传输视觉设置")]
    public float transmissionTime = 0.25f;
    public float stayTime = 1.0f;
    public float vanishTime = 0.15f;

    [Header("HDR 炫光颜色设置")]
    [GradientUsage(true)]
    public Gradient hdrLaserColor;

    public override void Execute(PlayerFloorInteraction player, AttributeFloor floor)
    {
        if (player.Hub.Energy != null)  player.Hub.Energy.AddButterflyEnergy(energyAmount);
        floor.PlayFloorFeedback();
        floor.Consume();

        // 🌟 注册表极速查找
        if (ButterflyAltar.ActiveAltars == null || ButterflyAltar.ActiveAltars.Count == 0) return;

        Transform nearestAltar = null;
        float minSqrDistance = Mathf.Infinity;
        Vector3 currentPos = floor.transform.position;

        foreach (ButterflyAltar altar in ButterflyAltar.ActiveAltars)
        {
            float sqrDistanceToAltar = (altar.transform.position - currentPos).sqrMagnitude;
            if (sqrDistanceToAltar < minSqrDistance)
            {
                minSqrDistance = sqrDistanceToAltar;
                nearestAltar = altar.transform;
            }
        }

        LineRenderer line = floor.GetComponent<LineRenderer>();
        if (nearestAltar != null && line != null)
        {
            line.colorGradient = hdrLaserColor;
            floor.StartCoroutine(EnergyTransmissionRoutine(line, currentPos, nearestAltar.position, nearestAltar.GetComponent<ButterflyAltar>()));
        }
    }

    public override void ExecuteExit(PlayerFloorInteraction player, AttributeFloor floor) { }

    private IEnumerator EnergyTransmissionRoutine(LineRenderer line, Vector3 startPos, Vector3 endPos, ButterflyAltar altar)
    {
        line.enabled = true;
        line.positionCount = 2;
        line.SetPosition(0, startPos);
        line.SetPosition(1, startPos);

        float elapsed = 0f;
        float originalWidth = line.widthMultiplier;

        while (elapsed < transmissionTime)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / transmissionTime;
            float easeOutT = 1f - Mathf.Pow(1f - t, 4f);
            line.SetPosition(1, Vector3.Lerp(startPos, endPos, easeOutT));
            yield return null;
        }
        line.SetPosition(1, endPos);

        // 🌟 真正给祭坛加能量
        if (altar != null) altar.AddEnergy(energyAmount);

        yield return new WaitForSeconds(stayTime);

        elapsed = 0f;
        while (elapsed < vanishTime)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / vanishTime;
            line.SetPosition(0, Vector3.Lerp(startPos, endPos, t));
            line.widthMultiplier = Mathf.Lerp(originalWidth, 0f, t);
            yield return null;
        }

        line.enabled = false;
        line.widthMultiplier = originalWidth;
    }
}