using System;
using System.Net;
using System.Collections.Generic;

namespace DeBox.Teleport.Transport
{    

    public class EndpointCollection
    {
        public readonly double SecondsTimeout;

        private readonly DateTime _epocDateTime;
        private readonly Dictionary<EndPoint, double> _endpointPings;
        private readonly Dictionary<EndPoint, BaseTeleportChannel[]> _endpointChannels;
        private readonly Func<BaseTeleportChannel>[] _channelCreators;

        public EndpointCollection(double secondsTimeout, Func<BaseTeleportChannel>[] channelCreators)
        {
            SecondsTimeout = secondsTimeout;
            _endpointPings = new Dictionary<EndPoint, double>();
            _endpointChannels = new Dictionary<EndPoint, BaseTeleportChannel[]>();
            _epocDateTime = new DateTime(1970, 1, 1);
            _channelCreators = channelCreators;
        }

        public void Ping(EndPoint endpoint)
        {            
            if (!_endpointPings.ContainsKey(endpoint))
            {

                _endpointChannels[endpoint] = CreateChannels();
            }
            _endpointPings[endpoint] = GetEpoc();           
        }

        private BaseTeleportChannel[] CreateChannels()
        {
            var channelCount = _channelCreators.Length;
            var channels = new BaseTeleportChannel[channelCount];
            for (int i = 0; i < channelCount; i++)
            {
                channels[i] = _channelCreators[i]();
            }
            return channels;
        }

        public BaseTeleportChannel[] GetChannelsOfEndpoint(EndPoint endpoint)
        {
            return _endpointChannels[endpoint];
        }

        public BaseTeleportChannel GetChannelOfEndpoint(EndPoint endpoint, byte channelId)
        {
            return _endpointChannels[endpoint][channelId];
        }

        public List<EndPoint> GetEndpoints()
        {
            Cleanup(SecondsTimeout);
            return new List<EndPoint>(_endpointPings.Keys);
        }

        private double GetEpoc()
        {
            TimeSpan t = DateTime.UtcNow - _epocDateTime;
            return t.TotalSeconds;
        }

        private void Cleanup(double timeout)
        {
            var endpointsToRemove = new List<EndPoint>();
            var now = GetEpoc();
            foreach (var pair in _endpointPings)
            {
                if (now - pair.Value > timeout)
                {
                    endpointsToRemove.Add(pair.Key);
                }
            }
            foreach (var ep in endpointsToRemove)
            {
                _endpointPings.Remove(ep);
                // TODO: Deinit channel
                _endpointChannels.Remove(ep);
            }
        }

    }
}
