namespace Chips
{
    
    public abstract class PassFilter
    {
        protected float _alpha;
        protected float _lastInput = 0.0f;
        protected float _lastOutput = 0.0f;
        public PassFilter(float cutoffFrequency, float sampleRate)
        {
            float dt = 1.0f / sampleRate;
            float rc = 1.0f / (2.0f * (float)Math.PI * cutoffFrequency);
            CalculateAlpha(dt,rc);
            Reset();
        }

        protected abstract void CalculateAlpha(float dt, float rc);

        public void Reset()
        {
            _lastInput = 0.0f;
            _lastOutput = 0.0f;
        }

    }

    public class HighPassFilter(float cutoffFrequency, float sampleRate) : PassFilter( cutoffFrequency,  sampleRate)
    {
        protected override void CalculateAlpha(float dt, float rc)
        {
            _alpha = rc / (rc + dt);
        }
        public float Process(float input)
        {
            _lastOutput = _alpha * (_lastOutput + input - _lastInput);
            _lastInput = input;
            return _lastOutput;
        }
    }

    public class LowPassFilter(float cutoffFrequency, float sampleRate) : PassFilter(cutoffFrequency, sampleRate)
    {
        protected override void CalculateAlpha(float dt, float rc)
        {
            _alpha = dt / (rc + dt);
        }
        public float Process(float input)
        {
            _lastOutput = _lastOutput + _alpha * (input - _lastOutput);
            _lastInput = input;
            return _lastOutput;
        }
    }

    public class SinglePoleIIRLowPassFilter: PassFilter
    {
         public SinglePoleIIRLowPassFilter(float cutoffFrequency, float sampleRate) : base(cutoffFrequency, sampleRate) 
        {
            CalculateAlpha(cutoffFrequency, sampleRate);
            Reset();
        }
        protected override void CalculateAlpha(float sampleRate, float cutoffFrequency)
        {
            // Equation is:
            // ɑ = 2 - cos(2π*(fc/fs)) - √((2-cos(2π*(fc/fs))² - 1)
            double omega_c = 2 * Math.PI * (cutoffFrequency / sampleRate);
            double cos_omega = Math.Cos(omega_c);
            _alpha = (float)(2 - cos_omega - Math.Sqrt(Math.Pow(2 - cos_omega, 2) - 1));
        }

        public float Process(float input)
        {
            _lastOutput = (1 - _alpha) * input + _alpha * _lastOutput;
            _lastInput = input;
            return _lastOutput;
        }

    }

}
