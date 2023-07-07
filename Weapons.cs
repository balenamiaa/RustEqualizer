using System.Numerics;

namespace RustEQ;

internal static class Weapons
{
    public const int NUM_ATTACHMENTS = 4;
    public const int NUM_WEAPONS = 13;

    public const byte ATTACHMENT_NONE = 0;
    public const byte ATTACHMENT_HOLO = 1;
    public const byte ATTACHMENT_SCOPE8 = 2;
    public const byte ATTACHMENT_SCOPE16 = 3;

    public const byte WEAPON_M249 = 0;
    public const byte WEAPON_AK47 = 1;
    public const byte WEAPON_LIGHTRIFLE = 2;
    public const byte WEAPON_M39 = 3;
    public const byte WEAPON_SEMIRIFLE = 4;
    public const byte WEAPON_MP5 = 5;
    public const byte WEAPON_CUSTOMSMG = 6;
    public const byte WEAPON_THOMPSON = 7;
    public const byte WEAPON_PYTHONREVOLVER = 8;
    public const byte WEAPON_M92 = 9;
    public const byte WEAPON_REVOLVER = 10;
    public const byte WEAPON_SEMIPISTOL = 11;
    public const byte WEAPON_NAILGUN = 12;

    public static readonly string[] AttachmentStringMap = new string[NUM_ATTACHMENTS] { "No Scope", "Holosight", "Scope x8", "Scope x16" };
    public static readonly string[] WeaponStringMap = new string[NUM_WEAPONS] { "M249", "Ak-47", "Light-Rifle", "M39", "Semi-AR", "Mp5", "Custom-Smg", "Thompson", "El Python", "M92", "Revolver", "Semi-Pistol", "Nailgun" };

    public static readonly float[] WEAPON_DEFAULT_ADSZOOM = new float[NUM_WEAPONS]
    {
            5.0f / 3.0f,
            5.0f / 3.0f,
            5.0f / 3.0f,
            5.0f / 4.0f,
            5.0f / 3.0f,
            5.0f / 3.0f,
            15.0f / 11.0f,
            15.0f / 11.0f,
            5.0f / 4.0f,
            15.0f / 13.0f,
            15.0f / 13.0f,
            15.0f / 13.0f,
            5.0f / 4.0f
    };

    public static readonly float[] ATTACHMENT_RCS_MULTIPLIER = new float[NUM_ATTACHMENTS]
    {
            1.0f,
            1.0f,
            0.8f,
            0.8f
    };

    public static readonly float[] ATTACHMENT_ZOOM = new float[NUM_ATTACHMENTS]
    {
            1.0f,
            2.0f,
            8.0f,
            16.0f
    };

    public static readonly bool[] WEAPON_ISCURVEBASED = new bool[NUM_WEAPONS]
    {
            false,
            true,
            true,
            false,
            false,
            true,
            true,
            true,
            false,
            false,
            false,
            false,
            false
    };

    public static readonly AnimationCurvePair[] WEAPON_ANIMATIONCURVE = new AnimationCurvePair[NUM_WEAPONS]
    {
            AnimationCurveFactory.NullCurve,
            AnimationCurveFactory.AkCurve,
            AnimationCurveFactory.LrCurve,
            AnimationCurveFactory.NullCurve,
            AnimationCurveFactory.NullCurve,
            AnimationCurveFactory.Mp5Curve,
            AnimationCurveFactory.SmgCurve,
            AnimationCurveFactory.SmgCurve,
            AnimationCurveFactory.NullCurve,
            AnimationCurveFactory.NullCurve,
            AnimationCurveFactory.NullCurve,
            AnimationCurveFactory.NullCurve,
            AnimationCurveFactory.NullCurve
    };

    public static readonly Vector2[] WEAPON_BASELINE_SCALAR = new Vector2[NUM_WEAPONS]
    {
            new(0, -5.5f),
            new(8, -30f),
            new(5, -12f),
            new(0, -6f),
            new(0, -5.5f),
            new(6, -10f),
            new(10, -15f),
            new(10, -15f),
            new(0, -15.5f),
            new(0, -7.5f),
            new(0, -4.5f),
            new(0, -7f),
            new(0, -4.5f)
    };

    public static readonly long[] WEAPON_TIMEPERSHOT_MS = new long[NUM_WEAPONS]
    {
            60000 / 500,
            60000 / 450,
            60000 / 500,
            200,
            175,
            100,
            100,
            60000 / 462,
            150,
            150,
            175,
            150,
            150
    };

    public static readonly bool[] WEAPON_ISAUTOMATIC = new bool[NUM_WEAPONS]
    {
            true,
            true,
            true,
            false,
            false,
            true,
            true,
            true,
            false,
            false,
            false,
            false,
            false
    };

    public static readonly int[] WEAPON_NUM_BULLETS = new int[NUM_WEAPONS]
    {
            100,
            30,
            30,
            20,
            16,
            30,
            24,
            20,
            6,
            15,
            8,
            10,
            16
    };

    public static readonly float[] WEAPON_MOVEMENT_PENALTY = new float[NUM_WEAPONS]
    {
            1.25f,
            0.0f,
            0.0f,
            0.5f,
            0.5f,
            0.0f,
            0.0f,
            0.0f,
            0.0f,
            0.0f,
            0.0f,
            0.5f,
            0.0f
    };

    public static readonly float[] WEAPON_ADSSCALE = new float[NUM_WEAPONS]
    {
            0.5f,
            1.0f,
            1.0f,
            0.5f,
            0.6f,
            1.0f,
            1.0f,
            1.0f,
            0.5f,
            0.5f,
            0.6f,
            0.6f,
            0.6f
    };
}