﻿using Sitecore.Commerce.Core;
using Sitecore.Commerce.Plugin.Pricing;
using System.Collections.Generic;

namespace Plugin.Plumber.Component.Decorator.Pipelines.Blocks.ViewValidators
{
    public class ValidatePriceCardViewBlock : ValidateEntityViewBaseBlock<PriceCard>
    {
        protected override List<string> GetApplicableViewNames(CommercePipelineExecutionContext context)
        {
            var viewsPolicy = context.GetPolicy<KnownPricingViewsPolicy>();
            return new List<string>() { viewsPolicy?.Master };
        }
    }
}
