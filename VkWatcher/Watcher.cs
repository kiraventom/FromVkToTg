using System;
using System.Collections.Generic;
using System.Timers;
using VkNet.Model;

namespace VkWatcher
{
    public class Watcher
    {
        public Watcher(IReadOnlyList<Group> groups)
        {
            _groups = groups;
            _timer = new Timer(_timerInterval);
        }

        private readonly IReadOnlyList<Group> _groups;
        private readonly double _timerInterval = TimeSpan.FromHours(1).TotalMilliseconds;
        private readonly Timer _timer;
        
        public void StartWatch()
        {
            
        }
    }
}