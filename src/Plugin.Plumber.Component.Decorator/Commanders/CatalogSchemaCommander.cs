﻿using Microsoft.Extensions.Logging;
using Plugin.Plumber.Component.Decorator.Attributes;
using Plugin.Plumber.Component.Decorator.Attributes.SellableItem;
using Plugin.Plumber.Component.Decorator.Pipelines;
using Plugin.Plumber.Component.Decorator.Pipelines.Arguments;
using Sitecore.Commerce.Core;
using Sitecore.Commerce.EntityViews;
using Sitecore.Commerce.Plugin.Catalog;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;

namespace Plugin.Plumber.Component.Decorator.Commanders
{
    /// <summary>
    ///     Helper class 
    /// </summary>
    public class ComponentViewCommander : CommerceCommander
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="serviceProvider"></param>
        public ComponentViewCommander(IServiceProvider serviceProvider) : base(serviceProvider)
        {
        }

        /// <summary>
        ///     Returns all registered component types  
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public async Task<List<Type>> GetAllComponentTypes(CommerceContext context)
        {
            var sellableItemComponentsArgument = new EntityViewComponentsArgument();
            sellableItemComponentsArgument = await this.Pipeline<IGetEntityViewComponentsPipeline>().Run(sellableItemComponentsArgument, context.GetPipelineContext());

            return sellableItemComponentsArgument.Components;
        }

        /// <summary>
        ///     Retrieves all component types applicable for the sellable item
        /// </summary>
        /// <param name="commerceEntity">Sellable item for which to get the applicable components</param>
        /// <param name="itemId"></param>
        /// <param name="context"></param>
        /// <returns></returns>
        public async Task<List<Type>> GetApplicableComponentTypes(CommerceEntity commerceEntity, string itemId, CommerceContext context)
        {
            // Get the item definition
            var catalogs = commerceEntity.GetComponent<CatalogsComponent>();

            // TODO: What happens if a sellableitem is part of multiple catalogs?
            var catalog = catalogs.GetComponent<CatalogComponent>();
            var itemDefinition = catalog.ItemDefinition;
 

            var sellableItemComponentsArgument = new EntityViewComponentsArgument();
            sellableItemComponentsArgument = await this.Pipeline<IGetEntityViewComponentsPipeline>().Run(sellableItemComponentsArgument, context.GetPipelineContext());

            var applicableComponentTypes = new List<Type>();
            foreach (var component in sellableItemComponentsArgument.Components)
            {
                System.Attribute[] attrs = System.Attribute.GetCustomAttributes(component);

                if (attrs.Any(attr => attr is AllSellableItemsAttribute) && commerceEntity is SellableItem)
                {
                    var sellableItemsAttribute = attrs.Single(attr => attr is SellableItemAttributeBase) as SellableItemAttributeBase;
                    var addToSellableItem = sellableItemsAttribute.AddToSellableItem;

                    if (IsApplicableComponent(itemId, addToSellableItem))
                    {
                        applicableComponentTypes.Add(component);
                    }
                }
                else if (attrs.Any(attr => attr is AddToItemDefinitionAttribute && ((AddToItemDefinitionAttribute)attr).ItemDefinition == itemDefinition))
                {
                    var sellableItemsAttribute = attrs.Single(attr => attr is AddToItemDefinitionAttribute) as AddToItemDefinitionAttribute;
                    var addToSellableItem = sellableItemsAttribute.AddToSellableItem;

                    if (IsApplicableComponent(itemId, addToSellableItem))
                    {
                        applicableComponentTypes.Add(component);
                    }
                }
                else if( attrs.Any(attr => attr is AddToEntityTypeAttribute && ((AddToEntityTypeAttribute)attr).EntityType == commerceEntity.GetType()))
                {
                    applicableComponentTypes.Add(component);
                }
                else if( attrs.Any(attr => attr is AddToAllEntityTypesAttribute))
                {
                    applicableComponentTypes.Add(component);
                }

            }

            return applicableComponentTypes;
        }

        private bool IsApplicableComponent(string itemId, AddToSellableItem addToSellableItem)
        {
            return (addToSellableItem == AddToSellableItem.SellableItemAndVariant) ||
                (addToSellableItem == AddToSellableItem.SellableItemOnly && string.IsNullOrEmpty(itemId)) ||
                (addToSellableItem == AddToSellableItem.VariantOnly && !string.IsNullOrEmpty(itemId));
        }

        /// <summary>
        ///    Gets a component of specified type from the Components property of the commerceEntity or creates a new 
        ///    component and adds it to the Components property of commerceEntity if the component does not exist.
        /// </summary>
        /// <param name="commerceEntity"></param>
        /// <param name="editedComponentType"></param>
        /// <returns></returns>
        public Sitecore.Commerce.Core.Component GetEditedComponent(CommerceEntity commerceEntity, Type editedComponentType)
        {
            Sitecore.Commerce.Core.Component component = commerceEntity.Components.SingleOrDefault(comp => comp.GetType() == editedComponentType);
            if (component == null)
            {
                component = (Sitecore.Commerce.Core.Component)Activator.CreateInstance(editedComponentType);
                commerceEntity.Components.Add(component);
            }

            return component;
        }

        /// <summary>
        ///     Sets the values from an edit view on the edited component. 
        /// </summary>
        /// <param name="properties"></param>
        /// <param name="editedComponentType"></param>
        /// <param name="editedComponent"></param>
        /// <param name="context"></param>
        public void SetPropertyValuesOnEditedComponent(List<ViewProperty> properties,
            Type editedComponentType,
            Sitecore.Commerce.Core.Component editedComponent,
            CommerceContext context)
        {
            // Map entity view properties to component
            var props = editedComponentType.GetProperties();

            foreach (var prop in props)
            {
                System.Attribute[] propAttributes = System.Attribute.GetCustomAttributes(prop);

                if (propAttributes.SingleOrDefault(attr => attr is PropertyAttribute) is PropertyAttribute propAttr)
                {
                    var fieldValue = properties.FirstOrDefault(x => x.Name.Equals(prop.Name, StringComparison.OrdinalIgnoreCase))?.Value;

                    TypeConverter converter = TypeDescriptor.GetConverter(prop.PropertyType);
                    if (converter.CanConvertFrom(typeof(string)) && converter.CanConvertTo(prop.PropertyType))
                    {
                        try
                        {
                            object propValue = converter.ConvertFromString(fieldValue);
                            prop.SetValue(editedComponent, propValue);
                        }
                        catch (Exception)
                        {
                            context.Logger.LogError($"Could not convert property '{prop.Name}' with value '{fieldValue}' to type '{prop.PropertyType}'");
                        }
                    }
                }
            }
        }

    }
}
