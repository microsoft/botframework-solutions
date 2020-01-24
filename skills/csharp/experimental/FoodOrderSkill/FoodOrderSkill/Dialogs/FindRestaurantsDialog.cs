using AdaptiveCards;
using Bot.Builder.Community.Adapters.Google;
using Bot.Builder.Community.Adapters.Google.Model;
using Bot.Builder.Community.Adapters.Google.Model.Attachments;
using FoodOrderSkill.Models;
using FoodOrderSkill.Services;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
using Microsoft.Bot.Schema;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Serialization;
using static FoodOrderSkill.Models.InternalTakeAwayModels;

namespace FoodOrderSkill.Dialogs
{
    public class FindRestaurantsDialog : SkillDialogBase
    {
        public TakeAwayService _takeAwayService;

        public FindRestaurantsDialog(
            IServiceProvider serviceProvider,
            IBotTelemetryClient telemetryClient,
            TakeAwayService takeAwayService)
            : base(nameof(FindRestaurantsDialog), serviceProvider, telemetryClient)
        {
            var steps = new WaterfallStep[]
            {
                RenderNearbyRestaurants,
                RenderRestaurantMenu,
                RenderOrderConfirmation,
                OrderSubmission
            };

            _takeAwayService = takeAwayService;

            AddDialog(new WaterfallDialog(nameof(FindRestaurantsDialog), steps));
            AddDialog(new TextPrompt(DialogIds.RenderNearbyRestaurants));
            AddDialog(new TextPrompt(DialogIds.renderRestaurantMenu));
            AddDialog(new TextPrompt(DialogIds.renderOrderConfirmation));
            AddDialog(new TextPrompt(DialogIds.renderOrderSubmission));
            InitialDialogId = nameof(FindRestaurantsDialog);
        }

        public async Task<DialogTurnResult> RenderNearbyRestaurants(WaterfallStepContext sc, CancellationToken cancellationToken)
        {
            var state = await StateAccessor.GetAsync(sc.Context, () => new SkillState());

            string neardyRestaurantsRaw = await _takeAwayService.getRestaurants();
            XmlSerializer serializer = new XmlSerializer(typeof(getRestaurants));
            StringReader rdr = new StringReader(neardyRestaurantsRaw);
            getRestaurants takeAwayRestaurants = (getRestaurants)serializer.Deserialize(rdr);

            state.AvailableTakeAwayRestaurants = takeAwayRestaurants;

            var imgUrlArray = new string[]
{
                    "https://www.singleplatform.com/wp-content/uploads/2018/12/5-Tips-for-Improving-Restaurant-Ambiance.jpg",
                    "https://images.squarespace-cdn.com/content/v1/5c586a93e666691041d4827c/1553679195118-LYFA5BT423UAFA64KEKF/ke17ZwdGBToddI8pDm48kPTrHXgsMrSIMwe6YW3w1AZ7gQa3H78H3Y0txjaiv_0fDoOvxcdMmMKkDsyUqMSsMWxHk725yiiHCCLfrh8O1z4YTzHvnKhyp6Da-NYroOW3ZGjoBKy3azqku80C789l0k5fwC0WRNFJBIXiBeNI5fKTrY37saURwPBw8fO2esROAxn-RKSrlQamlL27g22X2A/2019+-03+Restaurant+La+Palme+d%27Or+%C2%A9J.+Kelagopian+%2815%29.jpg?format=2500w",
                    "https://res.cloudinary.com/sagacity/image/upload/c_crop,h_3456,w_5184,x_0,y_0/c_limit,dpr_auto,f_auto,fl_lossy,q_80,w_1080/superdeluxe_owgtql.jpg",
                    "https://duyt4h9nfnj50.cloudfront.net/sku/82acc3366f2ebf288a0c258bbb80e4ab",
};

            Random random = new Random();


            if (sc.Context.Activity.ChannelId == "google")
            {
                Activity prompt = TemplateEngine.GenerateActivityForLocale("renderRestaurantsCarouselGoogle");
                List<OptionItem> restaurantOptions = new List<OptionItem>();
                for (int i = 0; i < takeAwayRestaurants.restaurants.Length; i++)
                {
                    restaurantOptions.Add(new OptionItem()
                    {
                        Title = takeAwayRestaurants.restaurants[i].name,
                        Image = new OptionItemImage() { AccessibilityText = "Item 4 image", Url = imgUrlArray[random.Next(0, 3)] },
                        OptionInfo = new OptionItemInfo() { Key = takeAwayRestaurants.restaurants[i].id, Synonyms = new List<string>() { "fourth" } },
                    });
                }

                var listAttachment = new ListAttachment(
                    "Make a selection",
                    restaurantOptions,
                    ListAttachmentStyle.Carousel);

                prompt.Attachments.Add(listAttachment);

                return await sc.PromptAsync(DialogIds.RenderNearbyRestaurants, new PromptOptions()
                {
                    Prompt = prompt
                });
            }
            else
            {
                //Activity prompt = TemplateEngine.GenerateActivityForLocale("MbfGetRestaurantsCarousel", new { restaurants = NormalizeRestaurants(takeAwayRestaurants.restaurants), channel = sc.Context.Activity.ChannelId });

                Activity prompt = TemplateEngine.GenerateActivityForLocale("renderRestaurantsCarouselGoogle");
                for (int i = 0; i < takeAwayRestaurants.restaurants.Length; i++)
                {
                    HeroCard card = new HeroCard()
                    {
                        Title = takeAwayRestaurants.restaurants[i].name,
                        Tap = new CardAction()
                        {
                            Type = "imBack",
                            Value = takeAwayRestaurants.restaurants[i].name,
                        },
                        Images = new List<CardImage>()
                        {
                            new CardImage()
                            {
                                Url = imgUrlArray[random.Next(0, 3)],
                            },

                        },
                    };
                    prompt.Attachments.Add(card.ToAttachment());
                    prompt.AttachmentLayout = AttachmentLayoutTypes.Carousel;
                }

                return await sc.PromptAsync(DialogIds.RenderNearbyRestaurants, new PromptOptions()
                {
                    Prompt = prompt,
                });
            }

        }

        public async Task<DialogTurnResult> RenderRestaurantMenu(WaterfallStepContext sc, CancellationToken cancellationToken)
        {
            var state = await StateAccessor.GetAsync(sc.Context, () => new SkillState());
            var selectedRestaurant = state.AvailableTakeAwayRestaurants.restaurants.First(r => r.name.ToLower() == sc.Result.ToString().ToLower());
            string restaurantInfoRaw = await _takeAwayService.getMenu(selectedRestaurant.id);
            XmlSerializer serializer = new XmlSerializer(typeof(getRestaurant));
            StringReader rdr = new StringReader(restaurantInfoRaw);
            getRestaurant selectedResterauntInfo = (getRestaurant)serializer.Deserialize(rdr);
            state.SelectedRestaurant = selectedResterauntInfo;

            List<getRestaurantRestaurantMenuCategoryProduct> productList = new List<getRestaurantRestaurantMenuCategoryProduct>();
            foreach (var category in selectedResterauntInfo.restaurant.menu.categories)
            {
                foreach (var item in category.products)
                {
                    productList.Add(item);
                }
            }
            state.AvailableProducts = productList;
            var promptActivity = TemplateEngine.GenerateActivityForLocale("renderRestaurantMenu");
            if (sc.Context.Activity.ChannelId == "google")
            {
                List<OptionItem> menuOptions = new List<OptionItem>();
                for (int i = 0; i < 10 && i < productList.Count; i++)
                {
                    menuOptions.Add(new OptionItem()
                    {
                        Title = productList[i].name,
                        Description = productList[i].description + " - " + productList[i].price,
                        OptionInfo = new OptionItemInfo() { Key = productList[i].name, Synonyms = new List<string>() { "fourth" } },
                    });
                }

                var listAttachment = new ListAttachment(
                   "Make a selection",
                   menuOptions,
                   ListAttachmentStyle.List);
                promptActivity.Attachments.Add(listAttachment);
            }
            else
            {
                AdaptiveCard menuCard = new AdaptiveCard();
                menuCard.Body.Add(new AdaptiveTextBlock()
                {
                    Text = "Please make a menu selection",
                    Size = AdaptiveTextSize.Medium,
                    Weight = AdaptiveTextWeight.Bolder,
                });
                for (int i = 0; i < 10 && i < productList.Count; i++)
                {
                    menuCard.Actions.Add(new AdaptiveSubmitAction()
                    {
                        Title = productList[i].name + " - " + productList[i].price,
                        Data = productList[i].name,
                    });
                }
                promptActivity.Attachments.Add(new Attachment()
                {
                    ContentType = AdaptiveCard.ContentType,
                    Content = JObject.FromObject(menuCard),
                });
            }

            return await sc.PromptAsync(DialogIds.renderRestaurantMenu, new PromptOptions()
            {
                Prompt = promptActivity,
            });
        }

        public async Task<DialogTurnResult> RenderOrderConfirmation(WaterfallStepContext sc, CancellationToken cancellationToken)
        {
            // Render summary of order this far and ask if order should be submitted or expanded upon
            var state = await StateAccessor.GetAsync(sc.Context, () => new SkillState());

            var selectedProduct = state.AvailableProducts.First(product => product.name.ToLower() == sc.Result.ToString().ToLower());
            Activity promptActivity = TemplateEngine.GenerateActivityForLocale("showOrderCard");

            if (sc.Context.Activity.ChannelId == "google")
            {
                var tableCardAttachment = new TableCardAttachment(
                        new TableCard()
                        {
                            Content = new TableCardContent()
                            {
                                ColumnProperties = new List<ColumnProperties>()
                                {
                                    new ColumnProperties() { Header = "Order Summary" },
                                    new ColumnProperties() { Header = "" },
                                },
                                Rows = new List<Row>()
                                {
                                    new Row() {
                                        Cells = new List<Cell>
                                        {
                                            new Cell { Text = "Restauraunt Name" },
                                            new Cell { Text = state.SelectedRestaurant.restaurant.name },
                                        },
                                    },
                                    new Row() {
                                        Cells = new List<Cell>
                                        {
                                            new Cell { Text = "Delivery Address" },
                                            new Cell { Text = "555 W Albertson pl" },
                                        },
                                    },
                                    new Row() {
                                        Cells = new List<Cell>
                                        {
                                            new Cell { Text = "Order Contents" },
                                            new Cell { Text = selectedProduct.name + " - $" + selectedProduct.price },
                                        },
                                    },
                                },
                            },
                        });
                promptActivity.Attachments.Add(tableCardAttachment);
            }
            else
            {
                AdaptiveCard orderSummaryCard = new AdaptiveCard();
                orderSummaryCard.Body.Add(new AdaptiveTextBlock()
                {
                    Text = "Order Details",
                    Size = AdaptiveTextSize.Medium,
                    Weight = AdaptiveTextWeight.Bolder,
                });
                orderSummaryCard.Body.Add(new AdaptiveColumnSet()
                {
                    Columns =
                    {
                        new AdaptiveColumn()
                    {
                        Width= AdaptiveColumnWidth.Stretch,
                        Items = new List<AdaptiveElement>()
                        {
                            new AdaptiveTextBlock()
                            {
                                Text = "Resaurant",
                            },
                            new AdaptiveTextBlock()
                            {
                                Text = "User",
                            },
                            new AdaptiveTextBlock()
                            {
                                Text = "Delivery Address",
                            },
                        },
                    },
                        new AdaptiveColumn()
                    {
                        Width= AdaptiveColumnWidth.Stretch,
                        Items = new List<AdaptiveElement>()
                        {
                            new AdaptiveTextBlock()
                            {
                                Text = state.SelectedRestaurant.restaurant.name,
                            },
                            new AdaptiveTextBlock()
                            {
                                Text = "pavolum",
                            },
                            new AdaptiveTextBlock()
                            {
                                Text = "555 W Albertson pl",
                            },
                        },
                    },
                    },

                });

                orderSummaryCard.Body.Add(new AdaptiveTextBlock()
                {
                    Text = "Order break down",
                    Size = AdaptiveTextSize.Medium,
                    Weight = AdaptiveTextWeight.Bolder,
                });
                orderSummaryCard.Body.Add(new AdaptiveColumnSet()
                {
                    Columns =
                    {
                        new AdaptiveColumn()
                    {
                        Width= AdaptiveColumnWidth.Stretch,
                        Items = new List<AdaptiveElement>()
                        {
                            new AdaptiveTextBlock()
                            {
                                Text = selectedProduct.name,
                            },
                        },
                    },
                        new AdaptiveColumn()
                    {
                        Width= AdaptiveColumnWidth.Stretch,
                        Items = new List<AdaptiveElement>()
                        {
                            new AdaptiveTextBlock()
                            {
                                Text = "$" + selectedProduct.price.ToString(),
                            },
                        },
                    },
                    },

                });

                List<AdaptiveAction> actionList = new List<AdaptiveAction>();
                orderSummaryCard.Actions.Add(new AdaptiveSubmitAction()
                {
                    Title = "Add to your order",
                    Data = "Add to your order"
                });

                orderSummaryCard.Actions.Add(new AdaptiveSubmitAction()
                {
                    Title = "Submit your order",
                    Data = "Submit your order"
                });


                promptActivity.Attachments.Add(new Attachment()
                {
                    ContentType = AdaptiveCard.ContentType,
                    Content = JObject.FromObject(orderSummaryCard),
                });
            }

            return await sc.PromptAsync(DialogIds.renderOrderConfirmation, new PromptOptions()
            {
                Prompt = promptActivity,
            });
        }

        public async Task<DialogTurnResult> OrderSubmission(WaterfallStepContext sc, CancellationToken cancellationToken)
        {
            await sc.Context.SendActivityAsync(TemplateEngine.GenerateActivityForLocale("orderSubmissionMessage"));
            //if (sc.Result != null && sc.Result.ToString() == "Submit your order")
            //{
            //    await sc.Context.SendActivityAsync(TemplateEngine.GenerateActivityForLocale("orderSubmissionMessage"));
            //}
            //else
            //{
            //    //TODO Implement adding to order flow
            //}
            return await sc.EndDialogAsync();
        }

        public NormalizedRestaurant[] NormalizeRestaurants(getRestaurantsRestaurant[] takeAwayRestaurants)
        {
            List<NormalizedRestaurant> result = new List<NormalizedRestaurant>() { };
            foreach (getRestaurantsRestaurant restaurant in takeAwayRestaurants)
            {
                result.Add(new NormalizedRestaurant() { Id = restaurant.id, Name = restaurant.name, Open = restaurant.open });
            }
            return result.ToArray();
        }

        private class DialogIds
        {
            public const string RenderNearbyRestaurants = "renderNearbyRestaurants";
            public const string renderRestaurantMenu = "renderRestaurantMenu";
            public const string renderOrderConfirmation = "renderOrderConfirmation";
            public const string renderOrderSubmission = "renderOrderSubmission";
        }
    }
}
