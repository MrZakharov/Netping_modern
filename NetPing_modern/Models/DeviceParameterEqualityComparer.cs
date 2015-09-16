using System.Collections.Generic;
using NetPing.Models;

namespace NetPing_modern.ViewModels
{
    public class DeviceParameterEqualityComparer : IEqualityComparer<DeviceParameter>
    {
        public bool Equals(DeviceParameter x, DeviceParameter y)
        {
            return string.Equals(x.Name.OwnNameFromPath, y.Name.OwnNameFromPath);
        }

        public int GetHashCode(DeviceParameter obj)
        {
            try
            {
                return obj.Name.OwnNameFromPath.GetHashCode();
            }
            catch
            {
                return obj.Device.OwnNameFromPath.GetHashCode();
            }
        }
    }
}