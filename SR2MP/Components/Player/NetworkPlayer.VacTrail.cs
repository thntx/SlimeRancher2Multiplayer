namespace SR2MP.Components.Player;

internal partial class NetworkPlayer
{
    private GameObject? vacTrailInstance;
    private bool vacTrailCreationFailed;

    /// <summary>
    /// Toggles the vac suction stream on a remote player. Driven by the
    /// existing VacRunningStart/VacRunningEnd FX packets.
    /// </summary>
    public void SetVacTrailActive(bool active)
    {
        if (IsLocal)
            return;

        if (!vacTrailInstance && active)
            CreateVacTrail();

        if (vacTrailInstance)
            vacTrailInstance!.SetActive(active);
    }

    private void CreateVacTrail()
    {
        if (vacTrailCreationFailed)
            return;

        try
        {
            var source = PlayerItemController?._vacuumItem?.VacFX;
            if (!source)
            {
                vacTrailCreationFailed = true;
                return;
            }

            var nozzle = FindVacNozzle();

            vacTrailInstance = Instantiate(source, nozzle, false);
            vacTrailInstance!.name = "SR2MP_VacTrail";
            vacTrailInstance.transform.localPosition = Vector3.zero;
            vacTrailInstance.transform.localRotation = Quaternion.identity;
            vacTrailInstance.SetActive(false);
        }
        catch (Exception ex)
        {
            vacTrailCreationFailed = true;
            SrLogger.LogWarning($"Could not create vac trail for remote player {ID}: {ex.Message}");
        }
    }

    private Transform FindVacNozzle()
    {
        Transform? best = null;
        var bestDepth = -1;

        foreach (var child in GetComponentsInChildren<Transform>(true))
        {
            var childName = child.name.ToLowerInvariant();
            if (!childName.Contains("vac"))
                continue;

            // Prefer the deepest vac bone so the stream starts at the muzzle.
            var depth = 0;
            for (var current = child; current && current != transform; current = current!.parent)
                depth++;

            if (childName.Contains("nozzle") || childName.Contains("muzzle") || childName.Contains("barrel"))
                depth += 100;

            if (depth <= bestDepth)
                continue;

            bestDepth = depth;
            best = child;
        }

        return best ? best! : transform;
    }
}
