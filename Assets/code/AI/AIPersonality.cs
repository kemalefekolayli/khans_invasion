public static class AIPersonality
{
    public static float GetExpandWeight(int aggressionLevel)
    {
        return RemapAggression(aggressionLevel, 0.15f, 0.75f);
    }

    public static float GetFortifyWeight(int aggressionLevel)
    {
        return RemapAggression(7 - aggressionLevel, 0.2f, 0.6f);
    }

    public static float GetIdleWeight(int aggressionLevel)
    {
        return RemapAggression(7 - aggressionLevel, 0.12f, 0.35f);
    }

    public static float GetExpandWeight(nationAgression aggression)
    {
        switch (aggression)
        {
            case nationAgression.heavyAgression: return 0.6f;
            case nationAgression.mediumAgression: return 0.4f;
            case nationAgression.lightAgression: return 0.2f;
            default: return 0.3f;
        }
    }

    public static float GetFortifyWeight(nationAgression aggression)
    {
        switch (aggression)
        {
            case nationAgression.heavyAgression: return 0.25f;
            case nationAgression.mediumAgression: return 0.35f;
            case nationAgression.lightAgression: return 0.5f;
            default: return 0.35f;
        }
    }

    public static float GetIdleWeight(nationAgression aggression)
    {
        switch (aggression)
        {
            case nationAgression.heavyAgression: return 0.15f;
            case nationAgression.mediumAgression: return 0.25f;
            case nationAgression.lightAgression: return 0.3f;
            default: return 0.25f;
        }
    }

    private static float RemapAggression(int aggressionLevel, float minValue, float maxValue)
    {
        int clamped = UnityEngine.Mathf.Clamp(aggressionLevel, 1, 6);
        float t = (clamped - 1) / 5f;
        return UnityEngine.Mathf.Lerp(minValue, maxValue, t);
    }
}
