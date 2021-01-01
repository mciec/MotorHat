using System;
using System.Device.I2c;
using System.Device.Pwm;
using Iot.Device.Pwm;

namespace Mciec.Drivers
{

    enum Direction
    {
        Left, Right
    };

    public class Motor
    {
        internal static double PwmMaxDutyCycle { get; } = (double)(4095.0 / 4096.0);
        public bool Initialized { get; internal set; } = false;
        internal PwmChannel Pwm;
        internal PwmChannel In1;
        internal PwmChannel In2;
        private double _speed;
        public double Speed
        {
            get => _speed;
            set
            {
                if (!Initialized)
                    throw new Exception("PWM channels not initialized.");
                double in1 = value >= 0 ? 0 : PwmMaxDutyCycle;
                if (Math.Sign(_speed) != Math.Sign(value))
                {
                    In1.DutyCycle = in1;
                    In2.DutyCycle = PwmMaxDutyCycle - in1;
                }
                _speed = value;
                Pwm.DutyCycle = Math.Abs(_speed);
            }
        }
    }

    public class MotorHat : IDisposable
    {
        private const int MaxNumberOfMotors = 16;
        private int _i2cAddress;
        private I2cConnectionSettings _i2cConnectionSettings = null;
        private I2cDevice _i2cDevice = null;
        private Pca9685 _pca9685 = null;
        private Motor[] _motor = new Motor[MaxNumberOfMotors];

        public MotorHat(int i2cAddress)
        {
            _i2cAddress = i2cAddress;
        }

        public void Init(int channelNumber)
        {
            if (channelNumber < 0 || channelNumber >= MaxNumberOfMotors)
                throw new Exception($"ChannelNumber can be [0...{MaxNumberOfMotors - 1}].");

            if (_motor[channelNumber] != null && _motor[channelNumber].Initialized)
                return;

            Motor motor;

            if (_motor[channelNumber] != null)
                motor = _motor[channelNumber];
            else
                motor = new Motor();

            int busId = 1;
            if (_i2cConnectionSettings == null)
                _i2cConnectionSettings = new I2cConnectionSettings(busId, _i2cAddress);
            if (_i2cDevice == null)
                _i2cDevice = I2cDevice.Create(_i2cConnectionSettings);
            if (_pca9685 == null)
                _pca9685 = new Pca9685(_i2cDevice, pwmFrequency: 50);

            motor.Pwm = _pca9685.CreatePwmChannel(channelNumber * 3);       //speed
            motor.In1 = _pca9685.CreatePwmChannel(channelNumber * 3 + 1);   //in1
            motor.In2 = _pca9685.CreatePwmChannel(channelNumber * 3 + 2);   //in2
            motor.Pwm.DutyCycle = 0.0;                                      //stop motor
            motor.Initialized = true;
            _motor[channelNumber] = motor;
        }

        public Motor this[int i]
        {
            get => _motor[i];
        }

        public void Dispose()
        {
            for (int i = 0; i < MaxNumberOfMotors; i++)
            {
                Motor motor = _motor[i];
                if (motor == null) return;
                if (motor.Speed != 0 && motor.Pwm != null) motor.Pwm.DutyCycle = 0;
                motor.Pwm?.Dispose();
                motor.In1?.Dispose();
                motor.In2?.Dispose();
            }
            _pca9685?.Dispose();
            _i2cDevice?.Dispose();
        }
    }
}


