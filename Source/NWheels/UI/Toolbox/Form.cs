﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using Hapil;
using NWheels.DataObjects;
using NWheels.Extensions;
using NWheels.TypeModel;
using NWheels.UI.Core;
using NWheels.UI.Uidl;

namespace NWheels.UI.Toolbox
{
    [DataContract(Namespace = UidlDocument.DataContractNamespace, Name = "Form")]
    public class Form<TEntity> : WidgetBase<Form<TEntity>, Form<TEntity>.IFormData, Empty.State>, IUidlForm
        where TEntity : class
    {
        private readonly List<string> _visibleFields;
        private readonly List<string> _hiddenFields;
        private UidlBuilder _uidlBuilder;
        private int _nextFieldGroupId;
        private bool _sectionsInsteadOfTabs;

        //-----------------------------------------------------------------------------------------------------------------------------------------------------

        public Form(string idName, ControlledUidlNode parent)
            : this(idName, parent, isNested: false)
        {
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------------------

        public Form(string idName, ControlledUidlNode parent, bool isNested)
            : base(idName, parent)
        {
            this.WidgetType = "Form";
            this.TemplateName = "Form";
            this.EntityName = MetadataCache.GetTypeMetadata(typeof(TEntity)).QualifiedName;
            this.Fields = new List<FormField>();
            this.UsePascalCase = true;

            _visibleFields = new List<string>();
            _hiddenFields = new List<string>();
            _nextFieldGroupId = 1;
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------------------

        public Form<TEntity> Field(
            Expression<Func<TEntity, object>> propertySelector,
            string label = null,
            FormFieldType type = FormFieldType.Default,
            FormFieldModifiers modifiers = FormFieldModifiers.None,
            Action<FormField> setup = null)
        {
            var field = new FormField(this, propertySelector.GetPropertyInfo().Name) {
                Label = label,
                FieldType = type,
                Modifiers = modifiers
            };

            if ( setup != null )
            {
                field.OnSetup += setup;
            }

            Fields.Add(field);
            return this;
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------------------

        public Form<TEntity> Field<TDerivedEntity>(
            Expression<Func<TDerivedEntity, object>> propertySelector,
            string label = null,
            FormFieldType type = FormFieldType.Default,
            FormFieldModifiers modifiers = FormFieldModifiers.None,
            Action<FormField> setup = null)
            where TDerivedEntity : TEntity
        {
            var field = new FormField(this, propertySelector.GetPropertyInfo().Name) {
                Label = label,
                FieldType = type,
                Modifiers = modifiers
            };

            if (setup != null)
            {
                field.OnSetup += setup;
            }

            Fields.Add(field);
            return this;
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------------------

        IUidlForm IUidlForm.Field<TDerivedEntity>(
            Expression<Func<TDerivedEntity, object>> propertySelector,
            string label,
            FormFieldType type,
            FormFieldModifiers modifiers,
            Action<FormField> setup)
        {
            var field = new FormField(this, propertySelector.GetPropertyInfo().Name) {
                Label = label,
                FieldType = type,
                Modifiers = modifiers
            };

            if (setup != null)
            {
                field.OnSetup += setup;
            }

            Fields.Add(field);
            return this;
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------------------

        IUidlForm IUidlForm.HideIdField()
        {
            return this.HideIdField();
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------------------

        public Form<TEntity> Range<T>(
            string label,
            Expression<Func<TEntity, T>> startPropertySelector,
            Expression<Func<TEntity, T>> endPropertySelector,
            params object[] presets)
        {
            var groupId = TakeNextFieldGroupId();
            var startField = FindOrAddField(startPropertySelector);
            var endField = FindOrAddField(endPropertySelector);

            startField.Label = label;
            startField.GroupId = groupId;
            startField.GroupIndex = 0;
            startField.Modifiers |= FormFieldModifiers.RangeStart;
            endField.GroupId = groupId;
            endField.GroupIndex = 1;
            endField.Modifiers |= FormFieldModifiers.RangeEnd;

            startField.StandardValues = presets.Select(v => v.ToString()).ToList();
            startField.StandardValuesExclusive = true;
            
            return this;
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------------------

        public Form<TEntity> ShowFields(params Expression<Func<TEntity, object>>[] propertySelectors)
        {
            _visibleFields.AddRange(propertySelectors.Select(e => e.GetPropertyInfo().Name));
            return this;
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------------------

        public Form<TEntity> HideFields(params Expression<Func<TEntity, object>>[] propertySelectors)
        {
            _hiddenFields.AddRange(propertySelectors.Select(e => e.GetPropertyInfo().Name));
            return this;
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------------------

        public Form<TEntity> HideIdField()
        {
            var metaType = MetadataCache.GetTypeMetadata(typeof(TEntity));

            if (metaType.EntityIdProperty != null)
            {
                _hiddenFields.Add(metaType.EntityIdProperty.Name);
            }

            return this;
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------------------

        public Form<TEntity> UseSectionsInsteadOfTabs()
        {
            _sectionsInsteadOfTabs = true;
            return this;
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------------------

        public Form<TEntity> Lookup<TLookupEntity>(
            Expression<Func<TEntity, object>> fieldSelector,
            Expression<Func<TLookupEntity, object>> lookupValueProperty,
            Expression<Func<TLookupEntity, object>> lookupDisplayProperty,
            Expression<Func<TLookupEntity, object>> lookupFilterProperty = null,
            object lookupFilterValue = null,
            bool applyDistinctToResults = true)
        {
            var field = FindOrAddField(fieldSelector);
            var lookupMetaType = MetadataCache.GetTypeMetadata(typeof(TLookupEntity));
            
            field.LookupEntityName = lookupMetaType.QualifiedName;
            field.LookupEntityContract = typeof(TLookupEntity);
            field.LookupValueProperty = lookupValueProperty.GetPropertyInfo().Name;
            field.LookupDisplayProperty = lookupDisplayProperty.GetPropertyInfo().Name;
            field.ApplyDistinctToLookup = applyDistinctToResults;

            field.FieldType = FormFieldType.Lookup;
            field.Modifiers = FormFieldModifiers.DropDown;

            if (lookupFilterProperty != null)
            {
                field.LookupQueryFilter = new List<FormFieldLookupFilter>() {
                    new FormFieldLookupFilter(
                        lookupFilterProperty.GetPropertyInfo().Name, 
                        ApplicationEntityService.QueryOptions.EqualOperator.TrimLead(":"), 
                        lookupFilterValue.ToStringOrDefault())
                };
            }

            return this;
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------------------

        public Form<TEntity> DisplayRelatedEntityAs<TLookupEntity>(
            Expression<Func<TEntity, TLookupEntity>> fieldSelector,
            Expression<Func<TLookupEntity, object>> lookupDisplayProperty)
        {
            var metaType = MetadataCache.GetTypeMetadata(typeof(TEntity));
            var metaProperty = metaType.GetPropertyByDeclaration(fieldSelector.GetPropertyInfo());
            var propertyAsObjectExpression = metaType.MakePropertyAsObjectExpression(metaProperty);

            var field = FindOrAddField((Expression<Func<TEntity, object>>)propertyAsObjectExpression);
            field.LookupDisplayProperty = lookupDisplayProperty.GetPropertyInfo().Name;

            return this;
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------------------

        public Form<TEntity> LookupSource<T>(
            Expression<Func<TEntity, T>> fieldSelector,
            Expression<Func<TEntity, ICollection<T>>> lookupSourceSelector)
        {
            var metaType = MetadataCache.GetTypeMetadata(typeof(TEntity));
            var fieldProperty = metaType.GetPropertyByDeclaration(fieldSelector.GetPropertyInfo());
            var lookupSourceProperty = metaType.GetPropertyByDeclaration(lookupSourceSelector.GetPropertyInfo());

            var field = FindOrAddField((Expression<Func<TEntity, object>>)metaType.MakePropertyAsObjectExpression(fieldProperty));
            field.FieldType = FormFieldType.Lookup;
            field.Modifiers = FormFieldModifiers.DropDown | FormFieldModifiers.LookupShowSelectNone;
            field.LookupSourceProperty = lookupSourceProperty.Name;

            if (!_hiddenFields.Contains(lookupSourceProperty.Name))
            {
                _hiddenFields.Add(lookupSourceProperty.Name);
            }

            return this;
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------------------

        public override IEnumerable<string> GetTranslatables()
        {
            var metaTypeTranslatables = new List<string>();

            for ( var metaType = MetadataCache.GetTypeMetadata(typeof(TEntity)) ; metaType != null ; metaType = metaType.BaseType )
            {
                metaTypeTranslatables.Add(metaType.QualifiedName);
            }

            return base.GetTranslatables()
                .Concat(metaTypeTranslatables)
                .Concat(Fields.Select(f => f.PropertyName))
                .Concat(Fields.Select(f => f.Label))
                .Concat(Fields.Where(f => f.StandardValues != null && f.StandardValuesExclusive).SelectMany(f => f.StandardValues))
                .Concat(Commands.Select(c => c.Text)); 
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------------------

        [DataMember]
        public string EntityName { get; set; }
        [DataMember]
        public object EntityId { get; set; }
        [DataMember]
        public List<FormField> Fields { get; set; }
        [DataMember]
        public bool UsePascalCase { get; set; }
        [DataMember]
        public bool IsModalPopup { get; set; }
        [DataMember]
        public bool IsInlineStyle { get; set; }
        [DataMember]
        public bool NeedsInitialModel { get; set; }
        [DataMember]
        public bool NeedsAuthorize { get; set; }
        [DataMember]
        public bool AutoSubmitOnChange { get; set; }
        [DataMember]
        public bool AutoRecalculateOnChange { get; set; }

        //-----------------------------------------------------------------------------------------------------------------------------------------------------

        public UidlNotification<TEntity> ModelSetter { get; set; }
        public UidlNotification StateResetter { get; set; }
        public UidlNotification<TEntity> Submitted { get; set; }

        //-----------------------------------------------------------------------------------------------------------------------------------------------------

        bool IUidlForm.UseSectionsInsteadOfTabs
        {
            get { return _sectionsInsteadOfTabs; }
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------------------

        protected override void DescribePresenter(PresenterBuilder<Form<TEntity>, IFormData, Empty.State> presenter)
        {
            //presenter.On(ModelSetter).AlterModel((alt => alt.Copy(vm => vm.Input).To(vm => vm.Data.Entity)));
            BuildFields(_uidlBuilder);
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------------------

        protected override void OnBuild(UidlBuilder builder)
        {
            _uidlBuilder = builder;
            builder.RegisterMetaType(typeof(TEntity));
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------------------

        public override IEnumerable<WidgetUidlNode> GetNestedWidgets()
        {
            return this.Fields.SelectMany(f => f.GetNestedWidgets());
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------------------

        void IUidlForm.ShowFields(params string[] propertyNames)
        {
            _visibleFields.AddRange(propertyNames);
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------------------

        void IUidlForm.HideFields(params string[] propertyNames)
        {
            _hiddenFields.AddRange(propertyNames);

            foreach ( var propertyName in propertyNames )
            {
                var field = this.Fields.FirstOrDefault(f => f.PropertyName == propertyName);
                
                if ( field != null )
                {
                    this.Fields.Remove(field);
                }
            }
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------------------

        private int TakeNextFieldGroupId()
        {
            return _nextFieldGroupId++;
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------------------

        private void BuildFields(UidlBuilder builder)
        {
            var metaType = builder.MetadataCache.GetTypeMetadata(typeof(TEntity));
            var fieldsToAdd = new List<string>();

            if ( _visibleFields.Count > 0 )
            {
                fieldsToAdd.AddRange(_visibleFields.Where(f => !fieldsToAdd.Contains(f)));
            }
            else
            {
                fieldsToAdd.AddRange(metaType.Properties.Where(ShouldAutoIncludeField).Select(p => p.Name).Where(f => !fieldsToAdd.Contains(f)));
            }

            fieldsToAdd = fieldsToAdd.Where(f => !_hiddenFields.Contains(f)).ToList();

            var preConfiguredFields = Fields;
            Fields = new List<FormField>();
            Fields.AddRange(fieldsToAdd.Select(f => preConfiguredFields.FirstOrDefault(pf => pf.PropertyName == f) ?? new FormField(this, f)));
            Fields.AddRange(preConfiguredFields.Where(pf => !Fields.Any(f => f.PropertyName == pf.PropertyName)));

            foreach ( var field in Fields )
            {
                field.Build(builder, this, metaType);
            }

            if ( _visibleFields.Count == 0 )
            {
                Fields.Sort((x, y) => y.OrderIndex.CompareTo(x.OrderIndex));
            }

            builder.BuildNodes(this.Fields.SelectMany(f => f.GetNestedWidgets()).Cast<AbstractUidlNode>().ToArray());
            builder.BuildNodes(this.Commands.Cast<AbstractUidlNode>().ToArray());

            this.AutoRecalculateOnChange = Fields.Any(f => f.MetaProperty != null && f.MetaProperty.IsCalculated);
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------------------

        private bool ShouldAutoIncludeField(IPropertyMetadata property)
        {
            //if ( property.Kind == PropertyKind.Relation && property.Relation.Kind.IsIn(RelationKind.CompositionParent, RelationKind.AggregationParent) )
            //{
            //    return false;
            //}

            return property.DefaultDisplayVisible.GetValueOrDefault(true);
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------------------

        private FormField FindOrAddField<T>(Expression<Func<TEntity, T>> fieldSelector)
        {
            var propertyName = fieldSelector.GetPropertyInfo().Name;
            var field = Fields.FirstOrDefault(f => f.PropertyName == propertyName);

            if ( field == null )
            {
                field = new FormField(this, propertyName);
                Fields.Add(field);
            }

            return field;
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------------------

        public interface IFormData
        {
            TEntity Entity { get; set; }
        }
    }

    //---------------------------------------------------------------------------------------------------------------------------------------------------------

    [DataContract(Namespace = UidlDocument.DataContractNamespace)]
    public class FormField
    {
        private readonly IUidlForm _ownerForm;

        //-----------------------------------------------------------------------------------------------------------------------------------------------------

        public FormField(IUidlForm ownerForm, string propertyName)
        {
            _ownerForm = ownerForm;
            this.PropertyName = propertyName;
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------------------

        public TWidget UseNestedWidget<TWidget>() 
            where TWidget : WidgetUidlNode
        {
            var widgetInstance = (TWidget)Activator.CreateInstance(typeof(TWidget), "Nested" + this.PropertyName + "Grid", (ControlledUidlNode)_ownerForm);;
            this.NestedWidget = widgetInstance;
            return widgetInstance;
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------------------

        [DataMember]
        public string PropertyName { get; set; }
        [DataMember]
        public string Label { get; set; }
        [DataMember]
        public UserAlertType AlertType { get; set; }
        [DataMember]
        public string Format { get; set; }
        [DataMember]
        public FormFieldType FieldType { get; set; }
        [DataMember]
        public FormFieldModifiers Modifiers { get; set; }
        [DataMember]
        public int? GroupId { get; set; }
        [DataMember]
        public int? GroupIndex { get; set; }
        [DataMember]
        public object InitialValue { get; set; }
        [DataMember]
        public bool HiddenIfEmpty { get; set; }
        [DataMember]
        public string LookupEntityName { get; set; }
        [DataMember]
        public string LookupSourceProperty { get; set; }
        [DataMember]
        public string LookupValueProperty { get; set; }
        [DataMember]
        public string LookupDisplayProperty { get; set; }
        [DataMember]
        public List<FormFieldLookupFilter> LookupQueryFilter { get; set; }
        [DataMember]
        public string ImageTypeProperty { get; set; }
        [DataMember]
        public string ImageContentProperty { get; set; }
        [DataMember]
        public bool ApplyDistinctToLookup { get; set; }
        [DataMember]
        public List<string> StandardValues { get; set; }
        [DataMember]
        public bool StandardValuesExclusive { get; set; }
        [DataMember]
        public bool StandardValuesMultiple { get; set; }
        [DataMember]
        public bool IsCalculated { get; set; }
        [DataMember, ManuallyAssigned]
        public WidgetUidlNode NestedWidget { get; set; }
        [DataMember, ManuallyAssigned]
        public WellKnownSemanticType Semantic { get; set; }
        [DataMember, ManuallyAssigned]
        public IPropertyValidationMetadata Validation { get; set; }
        [DataMember, ManuallyAssigned]
        public UidlAuthorization Authorization { get; set; }

        //-----------------------------------------------------------------------------------------------------------------------------------------------------

        internal IPropertyMetadata MetaProperty { get; private set; }
        internal int OrderIndex { get; private set; }
        internal Type LookupEntityContract { get; set; }
        internal Action<FormField> OnSetup { get; set; }

        //-----------------------------------------------------------------------------------------------------------------------------------------------------

        internal void Build(UidlBuilder builder, ControlledUidlNode parent, ITypeMetadata metaType)
        {
            var shouldSetDefaults = (this.FieldType == FormFieldType.Default);

            this.MetaProperty = metaType.GetPropertyByName(this.PropertyName);
            this.Validation = this.MetaProperty.Validation;
            this.Semantic = (this.MetaProperty.SemanticType != null ? this.MetaProperty.SemanticType.WellKnownSemantic : WellKnownSemanticType.None);

            if ( string.IsNullOrEmpty(this.Label) )
            {
                this.Label = GetDefaultFieldLabel(MetaProperty);
            }

            if ( shouldSetDefaults )
            {
                this.FieldType = GetDefaultFieldType();
            }

            if ( MetaProperty.Relation != null && MetaProperty.Relation.RelatedPartyType != null )
            {
                this.LookupEntityName = MetaProperty.Relation.RelatedPartyType.QualifiedName;
                this.LookupEntityContract = MetaProperty.Relation.RelatedPartyType.ContractType;

                if ( MetaProperty.Relation.RelatedPartyType.IsEntity )
                {
                    this.LookupValueProperty = MetaProperty.Relation.RelatedPartyType.EntityIdProperty.Name;
                    this.LookupDisplayProperty = MetaProperty.Relation.RelatedPartyType.DefaultDisplayProperties.Select(p => p.Name).FirstOrDefault();
                }

                if ( this.NestedWidget == null )
                {
                    this.NestedWidget = CreateNestedWidget(parent, MetaProperty.Relation.RelatedPartyType);
                }
            }

            if ( this.LookupEntityContract != null )
            {
                builder.RegisterMetaType(this.LookupEntityContract);
            }

            if ( this.MetaProperty.ClrType.IsEnum )
            {
                builder.RegisterMetaType(this.MetaProperty.ClrType);
                this.StandardValues = Enum.GetNames(this.MetaProperty.ClrType).ToList();
                this.StandardValuesExclusive = true;
            }

            if (MetaProperty.SemanticType != null && MetaProperty.SemanticType.HasStandardValues && this.StandardValues == null)
            {
                this.StandardValues = MetaProperty.SemanticType.GetStandardValues().Select(v => v.ToString()).ToList();
                this.StandardValuesExclusive = MetaProperty.SemanticType.StandardValuesExclusive;
            }

            if ( shouldSetDefaults )
            {
                this.Modifiers |= GetDefaultModifiers(this.FieldType);
            }

            this.OrderIndex = GetOrderIndex();

            builder.BuildManuallyInstantiatedNodes(NestedWidget);

            if ( OnSetup != null )
            {
                OnSetup(this);
            }

            IsCalculated = (MetaProperty != null && MetaProperty.IsCalculated);
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------------------

        internal IEnumerable<WidgetUidlNode> GetNestedWidgets()
        {
            if ( this.NestedWidget != null )
            {
                return new WidgetUidlNode[] { this.NestedWidget };
            }
            else
            {
                return new WidgetUidlNode[0];
            }
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------------------

        private List<FormFieldNestedWidget> CreateNestedWidgets(ControlledUidlNode parent)
        {
            var relatedMetaType = this.MetaProperty.Relation.RelatedPartyType;
            var allConcreteTypes = new List<ITypeMetadata>();

            if ( !relatedMetaType.IsAbstract )
            {
                allConcreteTypes.Add(relatedMetaType);
            }

            allConcreteTypes.AddRange(relatedMetaType.DerivedTypes.Where(t => !t.IsAbstract));

            var results = allConcreteTypes.Select(
                concreteType => new FormFieldNestedWidget() {
                    MetaType = concreteType.ContractType.AssemblyQualifiedNameNonVersioned(),
                    Widget = CreateNestedWidget(parent, concreteType)
                }).ToList();

            return results;
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------------------

        private WidgetUidlNode CreateNestedWidget(ControlledUidlNode parent, ITypeMetadata nestedMetaType)
        {
            WidgetUidlNode widgetInstance = null;
            Type widgetClosedType;

            switch ( this.FieldType )
            {
                case FormFieldType.Lookup:
                    if ( this.Modifiers == FormFieldModifiers.None || this.Modifiers.HasFlag(FormFieldModifiers.Ellipsis) )
                    {
                        widgetClosedType = typeof(DataGrid<>).MakeGenericType(nestedMetaType.ContractType);
                        var dataGridInstance = (DataGrid)Activator.CreateInstance(widgetClosedType, "Nested" + this.PropertyName + "Grid", parent);
                        dataGridInstance.EnableAutonomousQuery = true;
                        widgetInstance = dataGridInstance;
                    }
                    break;
                case FormFieldType.LookupMany:
                    widgetClosedType = typeof(LookupGrid<,>).MakeGenericType(nestedMetaType.EntityIdProperty.ClrType, nestedMetaType.ContractType);
                    widgetInstance = (WidgetUidlNode)Activator.CreateInstance(widgetClosedType, "Nested" + this.PropertyName + "Grid", parent);
                    break;
                case FormFieldType.InlineGrid:
                    widgetClosedType = typeof(Crud<>).MakeGenericType(nestedMetaType.ContractType);
                    widgetInstance = (WidgetUidlNode)Activator.CreateInstance(widgetClosedType, "Nested" + this.PropertyName + "Crud", parent, MetaProperty);
                    break;
                case FormFieldType.InlineForm:
                    widgetInstance = UidlUtility.CreateFormOrTypeSelector(nestedMetaType, "Nested" + this.PropertyName + "Form", parent, isInline: true);
                    break;
            }

            return widgetInstance;
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------------------

        private FormFieldType GetDefaultFieldType()
        {
            switch ( this.MetaProperty.Kind )
            {
                case PropertyKind.Scalar:
                    if ( MetaProperty.Role.IsIn(PropertyRole.Key, PropertyRole.Version) || MetaProperty.IsCalculated || 
                        !MetaProperty.Access.HasFlag(PropertyAccess.Write) )
                    {
                        return (MetaProperty.ContractPropertyInfo.CanWrite && MetaProperty.Access.HasFlag(PropertyAccess.Write) 
                            ? FormFieldType.Edit 
                            : FormFieldType.Label);
                    }
                    else if ( MetaProperty.Relation != null && MetaProperty.Relation.RelatedPartyType != null )
                    {
                        return (MetaProperty.IsCollection ? FormFieldType.LookupMany : FormFieldType.Lookup);
                    }
                    else
                    {
                        return (MetaProperty.ClrType.IsEnum || PropertyHasExclusiveStandardValues() ? FormFieldType.Lookup : FormFieldType.Edit);
                    }
                case PropertyKind.Part:
                    return (MetaProperty.IsCollection ? FormFieldType.InlineGrid : FormFieldType.InlineForm);
                case PropertyKind.Relation:
                    if ( MetaProperty.Relation.Kind == RelationKind.Composition || MetaProperty.Relation.RelatedPartyType.IsEntityPart )
                    {
                        return (MetaProperty.IsCollection ? FormFieldType.InlineGrid : FormFieldType.InlineForm);
                    }
                    else
                    {
                        return (MetaProperty.IsCollection ? FormFieldType.LookupMany : FormFieldType.Lookup);
                    }
                default:
                    return FormFieldType.Default;
            }
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------------------

        private bool PropertyHasExclusiveStandardValues()
        {
            return (MetaProperty.SemanticType != null && MetaProperty.SemanticType.HasStandardValues && MetaProperty.SemanticType.StandardValuesExclusive);
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------------------

        private FormFieldModifiers GetDefaultModifiers(FormFieldType type)
        {
            var value = GetBasicDefaultModifiers(type);

            if ( MetaProperty.Access == PropertyAccess.ReadOnly )
            {
                value |= FormFieldModifiers.ReadOnly;
            }

            return value;
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------------------

        private FormFieldModifiers GetBasicDefaultModifiers(FormFieldType type)
        {
            switch ( type )
            {
                case FormFieldType.Label:
                    return FormFieldModifiers.ReadOnly | (MetaProperty.IsCalculated ? FormFieldModifiers.None : FormFieldModifiers.System);
                case FormFieldType.Edit:
                    if ( MetaProperty.ClrType == typeof(Boolean) || MetaProperty.ClrType == typeof(Boolean?) )
                    {
                        return FormFieldModifiers.Checkbox;
                    }
                    else if ( MetaProperty.ClrType == typeof(DateTime) || MetaProperty.ClrType == typeof(DateTime?) )
                    {
                        return FormFieldModifiers.DateTimePicker;
                    }
                    else if ( MetaProperty.IsSensitive )
                    {
                        return FormFieldModifiers.Password;
                    }
                    return FormFieldModifiers.None;
                case FormFieldType.Lookup:
                    return (
                        StandardValues != null && StandardValues.Count > 0 ? 
                        FormFieldModifiers.DropDown | FormFieldModifiers.LookupShowSelectNone : 
                        FormFieldModifiers.Ellipsis);
                case FormFieldType.LookupMany:
                case FormFieldType.InlineForm:
                case FormFieldType.InlineGrid:
                    return (_ownerForm.UseSectionsInsteadOfTabs ? FormFieldModifiers.Section : FormFieldModifiers.Tab);
                default:
                    return FormFieldModifiers.None;
            }
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------------------

        private int GetOrderIndex()
        {
            int value = 0;

            if ( MetaProperty == MetaProperty.DeclaringContract.EntityIdProperty )
            {
                value |= 0x2000;
            }

            if ( Modifiers.HasFlag(FormFieldModifiers.System) )
            {
                value |= 0x1000;
            }

            if ( MetaProperty.DeclaringContract.DefaultDisplayProperties.Contains(MetaProperty) )
            {
                value |= 0x800;
            }

            if ( Modifiers.HasFlag(FormFieldModifiers.ReadOnly) )
            {
                value |= 0x400;
            }

            switch ( FieldType )
            {
                case FormFieldType.Lookup:
                    value |= 0x200;
                    break;
                case FormFieldType.Edit:
                    value |= 0x100;
                    break;
                case FormFieldType.InlineForm:
                    value |= 0x80;
                    break;
                case FormFieldType.InlineGrid:
                    value |= 0x40;
                    break;
                case FormFieldType.LookupMany:
                    value |= 0x20;
                    break;
            }

            return value;
        }

        //---------------------------------------------------------------------------------------------------------------------------------------------------------

        public static string GetDefaultFieldLabel(IPropertyMetadata metaProperty)
        {
            if ( metaProperty.Kind == PropertyKind.Scalar && metaProperty.Relation != null && metaProperty.Name.Length > 2 && metaProperty.Name.EndsWith("Id") )
            {
                return metaProperty.Name.TrimSuffix("Id");
            }
            else
            {
                return metaProperty.Name;
            }
        }
    }

    //---------------------------------------------------------------------------------------------------------------------------------------------------------
    
    public interface IUidlForm
    {
        void ShowFields(params string[] propertyNames);
        void HideFields(params string[] propertyNames);
        IUidlForm HideIdField();
        IUidlForm Field<TEntity>(
            Expression<Func<TEntity, object>> propertySelector,
            string label = null,
            FormFieldType type = FormFieldType.Default,
            FormFieldModifiers modifiers = FormFieldModifiers.None,
            Action<FormField> setup = null);
        bool UsePascalCase { get; set; }
        bool UseSectionsInsteadOfTabs { get; }
        List<FormField> Fields { get; }
        List<UidlCommandBase> Commands { get; }
        bool IsModalPopup { get; }
        bool NeedsInitialModel { get; set; }
        bool NeedsAuthorize { get; set; }
    }

    //---------------------------------------------------------------------------------------------------------------------------------------------------------

    [DataContract(Namespace = UidlDocument.DataContractNamespace)]
    public class FormFieldNestedWidget
    {
        [DataMember]
        public string MetaType { get; set; }
        [DataMember]
        public WidgetUidlNode Widget { get; set; }
    }

    //---------------------------------------------------------------------------------------------------------------------------------------------------------

    [DataContract(Namespace = UidlDocument.DataContractNamespace)]
    public class FormFieldLookupFilter
    {
        public FormFieldLookupFilter(string propertyName, string @operator, string stringValue)
        {
            PropertyName = propertyName;
            Operator = @operator;
            StringValue = stringValue;
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------------------

        [DataMember]
        public string PropertyName { get; set; }
        [DataMember]
        public string Operator { get; set; }
        [DataMember]
        public string StringValue { get; set; }
    }

    //---------------------------------------------------------------------------------------------------------------------------------------------------------

    public enum FormFieldType
    {
        Default = 0,
        Label = 10,
        Alert = 15,
        Edit = 20,
        FileUpload = 30,
        ImageUpload = 35,
        Lookup = 40,
        LookupMany = 50,
        InlineGrid = 60,
        InlineForm = 70
    }

    //---------------------------------------------------------------------------------------------------------------------------------------------------------

    [Flags]
    public enum FormFieldModifiers
    {
        None = 0x00,
        ReadOnly = 0x01,
        DropDown = 0x02,
        TypeAhead = 0x04,
        Ellipsis = 0x08,
        Section = 0x10,
        Tab = 0x20,
        Checkbox = 0x40,
        DateTimePicker = 0x80,
        Password = 0x100,
        Confirm = 0x200,
        LookupShowSelectAll = 0x400,
        LookupShowSelectNone = 0x800,
        RangeStart = 0x1000,
        RangeEnd = 0x2000,
        Memo = 0x4000,
        FlatStyle = 0x8000,
        Nullable = 0x10000,
        System = 0x40000000
    }

    //---------------------------------------------------------------------------------------------------------------------------------------------------------

    public enum FieldSize
    {
        Small = 10,
        Medium = 20,
        Large = 30
    }

    //---------------------------------------------------------------------------------------------------------------------------------------------------------

    public enum FieldSpecialName
    {
        None = 0,
        Id = 10,
        Type = 20
    }

    //---------------------------------------------------------------------------------------------------------------------------------------------------------

    public enum TimeRangePreset
    {
        Last5Minutes,
        Last15Minutes,
        Last30Minutes,
        LastHour,
        Last3Hours,
        Last4Hours,
        Last6Hours,
        Last12Hours,
        Last24Hours,
        Last3Days,
        Last7Days,
        Last30Days,
        Last3Months,
        Last6Months,
        Last12Months,
        Today,
        Yesterday,
        ThisWeek,
        LastWeek,
        ThisMonth,
        LastMonth,
        ThisQuarter,
        ThisYear,
        LastYear,
        AllTime
    }

    //---------------------------------------------------------------------------------------------------------------------------------------------------------

    public static class TimeRangePresetExtensions
    {
        private static readonly IReadOnlyList<TimeRangePreset> _s_allValues = 
            Enum.GetValues(typeof(TimeRangePreset)).Cast<TimeRangePreset>().ToArray();

        private static readonly IReadOnlyDictionary<TimeRangePreset, Func<DateTime, Interval<DateTime>>> _s_formulas = 
            new Dictionary<TimeRangePreset, Func<DateTime, Interval<DateTime>>>() {
                { TimeRangePreset.Last5Minutes, now => new Interval<DateTime>(now.AddMinutes(-5), now) },
                { TimeRangePreset.Last15Minutes, now => new Interval<DateTime>(now.AddMinutes(-15), now) },
                { TimeRangePreset.Last30Minutes, now => new Interval<DateTime>(now.AddMinutes(-30), now) },
                { TimeRangePreset.LastHour, now => new Interval<DateTime>(now.AddHours(-1), now) },
                { TimeRangePreset.Last3Hours, now => new Interval<DateTime>(now.AddHours(-3), now) },
                { TimeRangePreset.Last4Hours, now => new Interval<DateTime>(now.AddHours(-4), now) },
                { TimeRangePreset.Last6Hours, now => new Interval<DateTime>(now.AddHours(-6), now) },
                { TimeRangePreset.Last12Hours, now => new Interval<DateTime>(now.AddHours(-12), now) },
                { TimeRangePreset.Last24Hours, now => new Interval<DateTime>(now.AddHours(-24), now) },
                { TimeRangePreset.Last3Days, now => new Interval<DateTime>(now.AddDays(-3), now) },
                { TimeRangePreset.Last7Days, now => new Interval<DateTime>(now.AddDays(-7), now) },
                { TimeRangePreset.Last30Days, now => new Interval<DateTime>(now.AddDays(-30), now) },
                { TimeRangePreset.Last3Months, now => new Interval<DateTime>(now.AddMonths(-3), now) },
                { TimeRangePreset.Last6Months, now => new Interval<DateTime>(now.AddMonths(-6), now) },
                { TimeRangePreset.Last12Months, now => new Interval<DateTime>(now.AddMonths(-12), now) },
                { TimeRangePreset.Today, now => new Interval<DateTime>(now.Date, now.Date.AddDays(1).AddSeconds(-1)) },
                { TimeRangePreset.Yesterday, now => new Interval<DateTime>(now.Date.AddDays(-1), now.Date.AddSeconds(-1)) },
                { TimeRangePreset.ThisWeek, now => new Interval<DateTime>(now.StartOfWeek(DayOfWeek.Sunday), now.StartOfWeek(DayOfWeek.Sunday).AddDays(7).AddSeconds(-1)) },
                { TimeRangePreset.LastWeek, now => new Interval<DateTime>(now.StartOfWeek(DayOfWeek.Sunday), now.StartOfWeek(DayOfWeek.Sunday).AddDays(7).AddSeconds(-1)) },
                { TimeRangePreset.ThisMonth, now => new Interval<DateTime>(now.StartOfMonth(), now.StartOfMonth().AddMonths(1).AddSeconds(-1)) },
                { TimeRangePreset.LastMonth, now => new Interval<DateTime>(now.StartOfMonth().AddMonths(-1), now.StartOfMonth().AddSeconds(-1)) },
                { TimeRangePreset.ThisQuarter, now => new Interval<DateTime>(now.StartOfQuarter(), now.StartOfQuarter().AddMonths(3).AddSeconds(-1)) },
                { TimeRangePreset.ThisYear, now => new Interval<DateTime>(now.StartOfYear(), now.StartOfYear().AddYears(1).AddSeconds(-1)) },
                { TimeRangePreset.LastYear, now => new Interval<DateTime>(now.StartOfYear().AddYears(-1), now.StartOfYear().AddSeconds(-1)) },
                { TimeRangePreset.AllTime, now => new Interval<DateTime>(DateTime.MinValue, now) },
            };

        //-----------------------------------------------------------------------------------------------------------------------------------------------------

        public static Interval<DateTime> GetIntervalRelativeTo(this TimeRangePreset preset, DateTime utc)
        {
            return _s_formulas[preset](utc);
        }

        //-----------------------------------------------------------------------------------------------------------------------------------------------------
        
        public static IReadOnlyList<TimeRangePreset> AllValues()
        {
            return _s_allValues;
        }
    }
}
