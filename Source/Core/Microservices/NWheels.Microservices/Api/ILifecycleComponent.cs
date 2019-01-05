﻿namespace NWheels.Microservices.Api
{
    public interface ILifecycleComponent
    {
        void MicroserviceLoading();

        void Load();

        void MicroserviceLoaded();

        void MicroserviceActivating();

        void Activate();

        void MicroserviceActivated();

        void MicroserviceMaybeDeactivating();

        void MayDeactivate();

        void MicroserviceMaybeDeactivated();

        void MicroserviceMaybeUnloading();

        void MayUnload();

        void MicroserviceMaybeUnloaded();
    }
}
