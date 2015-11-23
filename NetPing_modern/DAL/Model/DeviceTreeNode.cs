using System.Collections.Generic;
using System.Linq;
using NetPing.Models;

namespace NetPing_modern.DAL
{
    internal class DeviceTreeNode
    {
        private readonly DeviceTreeNode _parentNode;
        private readonly DevicesTree _tree;
        private readonly Device _device;

        public DeviceTreeNode(DevicesTree tree, DeviceTreeNode parentNode, Device device)
        {
            _tree = tree;
            _parentNode = parentNode;
            _device = device;
        }

        public DeviceTreeNode Parent
        {
            get
            {
                return _parentNode;
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
            var path = _device.Name.Path;

            var childDevices = from dev in _tree.Devices where (dev.Name.Path.StartsWith(path) && dev.Name.Path != path) select dev;
            var level = _parentNode == null ? 3 : _parentNode.Device.Name.Level + 1;
            foreach (Device device in childDevices)
            {
                if (device.Name.Level == level)
                {
                    _nodes.Add(new DeviceTreeNode(_tree, this, device));
                }
            }
        }

        public Device Device
        {
            get
            {
                return _device;
            }
        }

        public string Name
        {
            get
            {
                return _device.Name.Name;
            }
        }

        public int Id
        {
            get
            {
                return _device.Id;
            }
        }
    }
}