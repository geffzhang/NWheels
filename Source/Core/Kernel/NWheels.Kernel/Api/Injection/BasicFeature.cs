using System;
using System.Diagnostics.CodeAnalysis;

namespace NWheels.Kernel.Api.Injection
{
    public abstract class BasicFeature : IFeatureLoader
    {
        void IFeatureLoader.ContributeConfigSections(IComponentContainerBuilder newComponents)
        {
            this.ContributeConfigSections(newComponents);
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------------------

        void IFeatureLoader.ContributeConfiguration(IComponentContainer existingComponents)
        {
            this.ContributeConfiguration(existingComponents);
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------------------

        void IFeatureLoader.ContributeComponents(IComponentContainer existingComponents, IComponentContainerBuilder newComponents)
        {
            this.ContributeComponents(existingComponents, newComponents);
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------------------

        void IFeatureLoader.ContributeAdapterComponents(IComponentContainer existingComponents, IComponentContainerBuilder newComponents)
        {
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------------------

        void IFeatureLoader.CompileComponents(IComponentContainer existingComponents)
        {
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------------------

        void IFeatureLoader.ContributeCompiledComponents(IComponentContainer existingComponents, IComponentContainerBuilder newComponents)
        {
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------------------

        public virtual void ContributeConfigSections(IComponentContainerBuilder newComponents)
        {
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------------------

        public virtual void ContributeConfiguration(IComponentContainer existingComponents)
        {
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------------------

        public virtual void ContributeComponents(IComponentContainer existingComponents, IComponentContainerBuilder newComponents)
        {
        }
    }
}
