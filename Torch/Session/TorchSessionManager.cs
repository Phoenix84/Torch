﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NLog;
using Sandbox.Game.World;
using Torch.API;
using Torch.API.Managers;
using Torch.API.Session;
using Torch.Managers;
using Torch.Session;

namespace Torch.Session
{
    /// <summary>
    /// Manages the creation and destruction of <see cref="TorchSession"/> instances for each <see cref="MySession"/> created by Space Engineers.
    /// </summary>
    public class TorchSessionManager : Manager, ITorchSessionManager
    {
        private static readonly Logger _log = LogManager.GetCurrentClassLogger();
        private TorchSession _currentSession;

        /// <inheritdoc />
        public event TorchSessionLoadDel SessionLoaded;

        /// <inheritdoc />
        public event TorchSessionLoadDel SessionUnloading;

        /// <inheritdoc/>
        public ITorchSession CurrentSession => _currentSession;

        private readonly HashSet<SessionManagerFactoryDel> _factories = new HashSet<SessionManagerFactoryDel>();

        public TorchSessionManager(ITorchBase torchInstance) : base(torchInstance)
        {
        }

        /// <inheritdoc/>
        public bool AddFactory(SessionManagerFactoryDel factory)
        {
            if (factory == null)
                throw new ArgumentNullException(nameof(factory), "Factory must be non-null");
            return _factories.Add(factory);
        }

        /// <inheritdoc/>
        public bool RemoveFactory(SessionManagerFactoryDel factory)
        {
            if (factory == null)
                throw new ArgumentNullException(nameof(factory), "Factory must be non-null");
            return _factories.Remove(factory);
        }

        private void LoadSession()
        {
            if (_currentSession != null)
            {
                _log.Warn($"Override old torch session {_currentSession.KeenSession.Name}");
                _currentSession.Detach();
            }

            _log.Info($"Starting new torch session for {MySession.Static.Name}");
            _currentSession = new TorchSession(Torch, MySession.Static);
            foreach (SessionManagerFactoryDel factory in _factories)
            {
                IManager manager = factory(CurrentSession);
                if (manager != null)
                    CurrentSession.Managers.AddManager(manager);
            }
            (CurrentSession as TorchSession)?.Attach();
            SessionLoaded?.Invoke(_currentSession);
        }

        private void UnloadSession()
        {
            if (_currentSession == null)
                return;
            SessionUnloading?.Invoke(_currentSession);
            _log.Info($"Unloading torch session for {_currentSession.KeenSession.Name}");
            _currentSession.Detach();
            _currentSession = null;
        }

        /// <inheritdoc/>
        public override void Attach()
        {
            MySession.AfterLoading += LoadSession;
            MySession.OnUnloaded += UnloadSession;
        }

        /// <inheritdoc/>
        public override void Detach()
        {
            _currentSession?.Detach();
            _currentSession = null;
            MySession.AfterLoading -= LoadSession;
            MySession.OnUnloaded -= UnloadSession;
        }
    }
}
