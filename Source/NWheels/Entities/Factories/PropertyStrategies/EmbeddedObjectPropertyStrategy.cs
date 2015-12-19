﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autofac;
using Hapil;
using Hapil.Operands;
using Hapil.Writers;
using NWheels.DataObjects;
using NWheels.DataObjects.Core.Factories;
using NWheels.Entities.Core;
using TT = Hapil.TypeTemplate;

namespace NWheels.Entities.Factories.PropertyStrategies
{
    public class EmbeddedObjectPropertyStrategy : PropertyImplementationStrategy
    {
        private readonly DomainObjectFactoryContext _context;
        private Type _objectImplementationType;
        private Field<TT.TProperty> _backingField;
        private Field<IComponentContext> _componentsField;

        //-----------------------------------------------------------------------------------------------------------------------------------------------------

        public EmbeddedObjectPropertyStrategy(
            PropertyImplementationStrategyMap ownerMap, 
            DomainObjectFactoryContext context, 
            IPropertyMetadata metaProperty)
            : base(ownerMap, context.BaseContext, context.MetadataCache, context.MetaType, metaProperty)
        {
            _context = context;
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------------------

        #region Overrides of PropertyImplementationStrategy

        protected override void OnBeforeImplementation(ImplementationClassWriter<TypeTemplate.TInterface> writer)
        {
            _backingField = writer.Field<TT.TProperty>("m_" + MetaProperty.Name);
            _objectImplementationType = FindImplementationType(MetaProperty.ClrType);
            _componentsField = writer.DependencyField<IComponentContext>("$components");
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------------------

        protected override void OnImplementContractProperty(ImplementationClassWriter<TypeTemplate.TInterface> writer)
        {
            Func<TemplatePropertyWriter, PropertyWriterBase.IPropertyWriterGetter> getter = null;
            Func<TemplatePropertyWriter, PropertyWriterBase.IPropertyWriterSetter> setter = null;

            if ( MetaProperty.ContractPropertyInfo.CanRead )
            {
                if ( MetaProperty == MetaType.EntityIdProperty )
                {
                    getter = p => p.Get(w =>
                    {
                        w.Return(Static.GenericFunc((obj, lz, val) => DomainModelRuntimeHelpers.EntityIdPropertyGetter<TT.TProperty>(obj, ref lz, ref val),
                            w.This<IDomainObject>(),
                            _context.ThisLazyLoaderField,
                            _backingField
                        ));
                    });

                }
                else
                {
                    getter = p => p.Get(w =>
                    {
                        w.Return(Static.GenericFunc((obj, lz, val) => DomainModelRuntimeHelpers.PropertyGetter<TT.TProperty>(obj, ref lz, ref val),
                            w.This<IDomainObject>(),
                            _context.ThisLazyLoaderField,
                            _backingField
                        ));
                    });
                }
            }

            if ( MetaProperty.ContractPropertyInfo.CanWrite )
            {
                setter = p => p.Set((w, value) =>
                {
                    Static.GenericVoid((obj, lz, bkf, val) => DomainModelRuntimeHelpers.PropertySetter<TT.TProperty>(obj, ref lz, out bkf, ref val),
                        w.This<IDomainObject>(),
                        _context.ThisLazyLoaderField,
                        _backingField,
                        value
                    );
                });
            }

            writer.Property(MetaProperty.ContractPropertyInfo).Implement(getter, setter);
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------------------

        protected override void OnImplementStorageProperty(ImplementationClassWriter<TypeTemplate.TInterface> writer)
        {
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------------------

        protected override void OnWritingImportStorageValue(MethodWriterBase writer, Operand<IEntityRepository> entityRepo, Operand<object[]> valueVector)
        {
            using ( TT.CreateScope<TT.TContract, TT.TImpl>(MetaProperty.ClrType, _objectImplementationType) )
            {
                _backingField.Assign(
                    Static.GenericFunc((r, v, f) => DomainModelRuntimeHelpers.ImportEmbeddedDomainObject<TT.TContract, TT.TImpl>(r, v, f),
                        entityRepo,
                        valueVector.ItemAt(MetaProperty.PropertyIndex),
                        writer.Delegate<TT.TImpl>(w => {
                            w.Return(_context.DomainObjectFactoryField.Func<TT.TContract>(x => x.CreateDomainObjectInstance<TT.TContract>).CastTo<TT.TImpl>());                                    
                        })
                    )
                    .CastTo<TT.TProperty>()
                );
            }
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------------------

        protected override void OnWritingExportStorageValue(MethodWriterBase writer, Operand<IEntityRepository> entityRepo, Operand<object[]> valueVector)
        {
            valueVector.ItemAt(MetaProperty.PropertyIndex).Assign(_backingField.CastTo<object>());
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------------------

        protected override void OnWritingInitializationConstructor(MethodWriterBase writer, Operand<IComponentContext> components, params IOperand[] args)
        {
            if ( MetaProperty.Relation.RelatedPartyType.IsEntityPart || !MetaProperty.ContractPropertyInfo.CanWrite )
            {
                using ( TT.CreateScope<TT.TImpl>(_objectImplementationType) )
                {
                    _backingField.Assign(writer.New<TT.TImpl>(components).CastTo<TT.TProperty>());
                }
            }
        }

        #endregion
    }
}
