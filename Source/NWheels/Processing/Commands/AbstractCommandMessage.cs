﻿using System;
using System.Collections.Generic;
using System.Security.Principal;
using NWheels.Authorization;
using NWheels.Processing.Messages;
using NWheels.UI;

namespace NWheels.Processing.Commands
{
    public abstract class AbstractCommandMessage : MessageObjectBase
    {
        protected AbstractCommandMessage()
        {
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------------------

        protected AbstractCommandMessage(IFramework framework, ISession session, bool isSynchronous)
            : base(framework)
        {
            Principal = session.UserPrincipal;
            Session = session;
            UIContext = UIOperationContext.Current;
            IsSynchronous = isSynchronous;
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------------------

        public IDisposable UseOptionalUIContextOnCurrentThread()
        {
            if (this.UIContext != null)
            {
                return this.UIContext.UseOnCurrentThread();
            }

            return null;
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------------------

        public bool HasFaultResult()
        {
            return (Result != null && Result.Success == false && !string.IsNullOrEmpty(Result.FaultCode));
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------------------

        public abstract bool CheckAuthorization(out bool authenticationRequired);

        //-----------------------------------------------------------------------------------------------------------------------------------------------------

        public virtual Dictionary<string, object> GetParameters()
        {
            return null;
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------------------

        #region Overrides of Object

        public override string ToString()
        {
            return CommandString;
        }

        #endregion

        //-----------------------------------------------------------------------------------------------------------------------------------------------------

        public IPrincipal Principal { get; private set; }
        public ISession Session { get; private set; }
        public UIOperationContext UIContext { get; private set; }
        public bool IsSynchronous { get; private set; }

        //-----------------------------------------------------------------------------------------------------------------------------------------------------

        public virtual string AuditName
        {
            get
            {
                return CommandString;
            }
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------------------

        public abstract string CommandString { get; }

        //-----------------------------------------------------------------------------------------------------------------------------------------------------
        
        public CommandResultMessage Result { get; set; }
    }
}
