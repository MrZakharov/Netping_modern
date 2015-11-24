using System.Collections.Generic;
using NetPing.Models;

namespace NetPing_modern.DAL
{
    internal class DevicesTree
    {
        private readonly IEnumerable<Device> _devices; 

        public DevicesTree(IEnumerable<Device> devices)
        {
            _devices = devices;
        }

        public IEnumerable<Device> Devices
        {
            get
            {
                return _devices;
            }
        }

        private List<DeviceTreeNode> _nodes;
        public List<DeviceTreeNode> Nodes
        {
            get
            {
                if (_nodes == null)
                {
                    BuildNodes();
                }
                return _nodes;
            }
        }

        private void BuildNodes()
        {
            _nodes = new List<DeviceTreeNode>();
            foreach (Device device in Devices)
            {
                if (device.Name.Level == 2)
                {
                    _nodes.Add(new DeviceTreeNode(this, null, device));
                }
            }
        }
    }
}