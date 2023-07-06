namespace RustEQ;

using System.Reflection;
using System.Text.Json;

public struct KeyPoint
{
    public double value;
    public double slope;
    public double time;

    public KeyPoint(double value, double slope, double time)
    {
        this.value = value;
        this.slope = slope;
        this.time = time;
    }
}

public struct AnimationCurveCoefficients
{
    public double a;
    public double b;
    public double c;
    public double d;

    public AnimationCurveCoefficients(double a, double b, double c, double d)
    {
        this.a = a;
        this.b = b;
        this.c = c;
        this.d = d;
    }
}

public struct AnimationCurve
{
    public List<KeyPoint> keypoints;
    public List<AnimationCurveCoefficients> coefficients;

    public AnimationCurve(int num_keypoints)
    {
        keypoints = new List<KeyPoint>(num_keypoints);
        coefficients = new List<AnimationCurveCoefficients>(num_keypoints - 1);
    }

    public AnimationCurve()
    {
        keypoints = new List<KeyPoint>();
        coefficients = new List<AnimationCurveCoefficients>();
    }

    /// <summary>
    /// Calculates the coefficients for the animation curve based on the current key points.
    /// </summary>
    /// <exception cref="Exception">Thrown when the length of keypoints is zero or the length of coefficients is not equal to length of keypoints minus one.</exception>
    public readonly void SetupCoefficients()
    {
        if (keypoints.Count == 0)
        {
            throw new Exception("Length of keypoints cannot be zero.");
        }
        if (coefficients.Count != keypoints.Count - 1)
        {
            throw new Exception("Length of coefficients must be equal to length of keypoints minus one.");
        }

        int idx = 2;
        while (idx <= keypoints.Count)
        {
            int current_idx = idx;
            int prev_idx = current_idx - 1;

            KeyPoint k1 = keypoints[prev_idx - 1];
            KeyPoint k2 = keypoints[current_idx - 1];

            double x(KeyPoint key) => key.time;
            double y(KeyPoint key) => key.value;
            double m(KeyPoint key) => key.slope;

            double denom = x(k1) * x(k1) * x(k1) - x(k2) * x(k2) * x(k2) + 3 * x(k1) * x(k2) * (x(k2) - x(k1));
            double div(double val) => val / denom;

            double a = div(
                (m(k1) + m(k2)) *
                (x(k1) - x(k2)) +
                2 * (y(k2) - y(k1))
            );

            double b = div(
                2 * (m(k1) * x(k2) * x(k2) - m(k2) * x(k1) * x(k1)) -
                m(k1) * x(k1) * x(k1) +
                m(k2) * x(k2) * x(k2) +
                (m(k2) - m(k1)) * x(k2) * x(k1) +
                3 * (x(k1) + x(k2)) * (y(k1) - y(k2))
            );

            double c = div(
                m(k2) * x(k1) * x(k1) * x(k1) -
                m(k1) * x(k2) * x(k2) * x(k2) +
                x(k1) * x(k2) * (x(k1) * (2 * m(k1) + m(k2)) - x(k2) * (m(k1) + 2 * m(k2))) +
                6 * x(k1) * x(k2) * (y(k2) - y(k1))
            );

            double d = div(
                (x(k1) * x(k2) * x(k2) - x(k2) * x(k1) * x(k1)) * (m(k1) * x(k2) + m(k2) * x(k1)) -
                y(k1) * x(k2) * x(k2) * x(k2) +
                y(k2) * x(k1) * x(k1) * x(k1) +
                3 * x(k1) * x(k2) * (x(k2) * y(k1) - x(k1) * y(k2))
            );

            coefficients[prev_idx - 1] = new AnimationCurveCoefficients(a, b, c, d);

            idx += 1;
        }
    }

    /// <summary>
    /// Evaluates the animation curve at the given time.
    /// </summary>
    /// <param name="t">The time at which to evaluate the curve.</param>
    /// <returns>The value of the curve at the given time.</returns>
    public readonly double Evaluate(double t)
    {
        int N = keypoints.Count;

        int i = 1;
        while (i <= N)
        {
            if (t <= keypoints[i - 1].time)
            {
                break;
            }

            i += 1;
        }
        i = i - 1;

        if (i == 0)
        {
            return keypoints[0].value;
        }
        if (i == N)
        {
            return keypoints[i - 1].value;
        }

        AnimationCurveCoefficients coeff = coefficients[i - 1];
        double a = coeff.a;
        double b = coeff.b;
        double c = coeff.c;
        double d = coeff.d;

        return a * t * t * t + b * t * t + c * t + d;
    }
}


public struct AnimationCurvePair
{
    public AnimationCurve yaw;
    public AnimationCurve pitch;

    public AnimationCurvePair(AnimationCurve yaw, AnimationCurve pitch)
    {
        this.yaw = yaw;
        this.pitch = pitch;
    }
}

public static class AnimationCurveFactory
{
    public static AnimationCurve FromJson(string jsonStr)
    {
        JsonElement json = JsonSerializer.Deserialize<JsonElement>(jsonStr);
        int keypointCount = json.GetArrayLength();

        AnimationCurve curve = new(keypointCount);

        for (int i = 0; i < keypointCount; i++)
        {
            double value = json[i].GetProperty("value").GetDouble();
            double time = json[i].GetProperty("time").GetDouble();
            double slope = json[i].GetProperty("inSlope").GetDouble();
            curve.keypoints[i] = new KeyPoint(value, slope, time);
        }

        curve.SetupCoefficients();

        return curve;
    }


    public static readonly AnimationCurvePair AkCurve = new(
        FromJson(Assembly.GetExecutingAssembly().ReadResource("ak47_yaw.json")),
        FromJson(Assembly.GetExecutingAssembly().ReadResource("ak47_pitch.json"))
    );

    public static readonly AnimationCurvePair LrCurve = new(
        FromJson(Assembly.GetExecutingAssembly().ReadResource("l96_yaw.json")),
        FromJson(Assembly.GetExecutingAssembly().ReadResource("l96_pitch.json"))
    );

    public static readonly AnimationCurvePair Mp5Curve = new(
        FromJson(Assembly.GetExecutingAssembly().ReadResource("mp5_yaw.json")),
        FromJson(Assembly.GetExecutingAssembly().ReadResource("mp5_pitch.json"))
    );

    public static readonly AnimationCurvePair SmgCurve = new(
        FromJson(Assembly.GetExecutingAssembly().ReadResource("smg_yaw.json")),
        FromJson(Assembly.GetExecutingAssembly().ReadResource("smg_pitch.json"))
    );

    public static readonly AnimationCurvePair NullCurve = new(new(), new());
}







