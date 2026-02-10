public static class AIPersonality
{
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
}
