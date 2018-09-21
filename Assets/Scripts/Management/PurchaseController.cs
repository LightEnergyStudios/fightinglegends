
using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Purchasing;


namespace FightingLegends
{
	// implements IStoreListener interface to enable receiving messages from Unity IAP
	public class PurchaseController : MonoBehaviour, IStoreListener
	{
		private IStoreController storeController = null;          // the Unity IAP system
		private IExtensionProvider storeExtensionProvider = null; // the store-specific IAP subsystems

		// general product identifiers for the consumable, non-consumable, and subscription products.
		// used in code to reference which product to purchase and when defining the Product Identifiers in the store.
		public const string Coins100ProductID = "com.burningheart.fightinglegends.100coins";   
		public const string Coins1000ProductID = "com.burningheart.fightinglegends.1000coins";   
		public const string Coins10000ProductID = "com.burningheart.fightinglegends.10000coins";   

		private FightManager fightManager;
		private SurvivalSelect fighterSelect;		// includes level, xp, power-ups etc

//		private bool internetReachable = false;

		private Product productToPurchase = null;


		private void Awake()
		{
			if (! IsInitialised)
				InitialisePurchasing();

//			internetReachable = (Application.internetReachability != NetworkReachability.NotReachable);
		}
			

		internal bool IsInitialised
		{
			get { return storeController != null && storeExtensionProvider != null; }
		}

		internal void InitialisePurchasing() 
		{
			// Create a builder, first passing in a suite of Unity provided stores.
			var builder = ConfigurationBuilder.Instance(StandardPurchasingModule.Instance());

			// Add a product to sell / restore by way of its identifier, associating the general identifier
			// with its store-specific identifiers
			builder.AddProduct(Coins100ProductID, ProductType.Consumable);
			builder.AddProduct(Coins1000ProductID, ProductType.Consumable);
			builder.AddProduct(Coins10000ProductID, ProductType.Consumable);

			productToPurchase = null;

			// Kick off the remainder of the set-up with an asynchronous call, passing the configuration 
			// and this class' instance. Expect a response either in OnInitialized or OnInitializeFailed
			UnityPurchasing.Initialize(this, builder);
		}


		// external entry point
		public void PurchaseProductID(string productId)
		{
			productToPurchase = null;

			if (IsInitialised)
			{
				// ... look up the Product reference with the general product identifier and the Purchasing system's products collection
				productToPurchase = storeController.products.WithID(productId);

				// If the look up found a product for this device's store and that product is ready to be sold ... 
				if (productToPurchase != null && productToPurchase.availableToPurchase)
				{
					var productDesc = productToPurchase.metadata.localizedDescription;
					var productCurrency = productToPurchase.metadata.isoCurrencyCode;
					var productPrice = productToPurchase.metadata.localizedPriceString;

//					FightManager.GetConfirmation(string.Format(FightManager.Translate("confirmPurchase"), productDesc, productCurrency, productPrice), 0, InitiatePurchase);
//					FightManager.GetConfirmation(FightManager.Translate("confirmPurchase2"), 0, InitiatePurchase);

//					Debug.Log(string.Format("PurchaseProductID: Purchasing product asychronously: desc '{0}' currency '{1}' price '{2}'", productDesc, productCurrency, productPrice) + " : productId = " + productId);

//					// ... buy the product. Expect a response either through ProcessPurchase or OnPurchaseFailed asynchronously
					InitiatePurchase();
				}
				else
				{
					Debug.Log("BuyProductID FAILED: Product not found or is not available for purchase");
				}
			}
			else
			{
				// ... report the fact Purchasing has not succeeded initializing yet. Consider waiting longer or retrying initialization
				Debug.Log("BuyProductID FAILED: Not initialized.");
			}
		}

		private void InitiatePurchase()
		{
			// buy the product - expect a response either through ProcessPurchase or OnPurchaseFailed asynchronously
//			Debug.Log(string.Format("InitiatePurchase: productToPurchase = '{0}'", productToPurchase));

			if (productToPurchase != null)
			{
				Debug.Log(string.Format("InitiatePurchase: '{0}'", productToPurchase.definition.id));
				storeController.InitiatePurchase(productToPurchase);
			}
		}


		// Restore purchases previously made by this customer. Some platforms automatically restore purchases, like Google. 
		// Apple currently requires explicit purchase restoration for IAP, conditionally displaying a password prompt
		public void RestorePurchases()
		{
			if (! IsInitialised)
			{
				// ... report the situation and stop restoring. Consider either waiting longer, or retrying initialization
				Debug.Log("RestorePurchases FAIL. Not initialized.");
				return;
			}
				
			if (Application.platform == RuntimePlatform.IPhonePlayer)
			{
				// ... begin restoring purchases
				Debug.Log("RestorePurchases started ...");

				// Fetch the Apple store-specific subsystem.
				var apple = storeExtensionProvider.GetExtension<IAppleExtensions>();

				// Begin the asynchronous process of restoring purchases. Expect a confirmation response in 
				// the Action<bool> below, and ProcessPurchase if there are previously purchased products to restore
				apple.RestoreTransactions((result) => {
					// The first phase of restoration. If no more responses are received on ProcessPurchase then 
					// no purchases are available to be restored
					Debug.Log("RestorePurchases continuing: " + result + ". If no further messages, no purchases available to restore.");
				});
			}
			else
			{
				// Not running on an Apple device. No work is necessary to restore purchases.
				Debug.Log("RestorePurchases not supported on this platform " + Application.platform);
			}
		}


		#region callbacks

		//  
		// --- IStoreListener async callbacks
		//

		public void OnInitialized(IStoreController controller, IExtensionProvider extensions)
		{
			// Purchasing has succeeded initializing. Collect our Purchasing references.
			Debug.Log("PurchaseController: OnInitialized: SUCCESS!");

			// Overall Purchasing system, configured with products for this application.
			storeController = controller;
			// Store specific subsystem, for accessing device-specific store features.
			storeExtensionProvider = extensions;
		}


		public void OnInitializeFailed(InitializationFailureReason error)
		{
			// Purchasing set-up has not succeeded. Check error for reason. Consider sharing this reason with the user.
			Debug.Log("PurchaseController: OnInitializeFailed: reason: " + error);
		}


		public PurchaseProcessingResult ProcessPurchase(PurchaseEventArgs args) 
		{
			// The consumable item has been successfully purchased, add coins to the player's in-game coins.
			if (String.Equals(args.purchasedProduct.definition.id, Coins100ProductID, StringComparison.Ordinal))
			{
				Debug.Log(string.Format("ProcessPurchase: SUCCESS - Product: '{0}'", args.purchasedProduct.definition.id));
				FightManager.Coins += 100;
			}
			else if (String.Equals(args.purchasedProduct.definition.id, Coins1000ProductID, StringComparison.Ordinal))
			{
				Debug.Log(string.Format("ProcessPurchase: SUCCESS - Product: '{0}'", args.purchasedProduct.definition.id));
				FightManager.Coins += 1000;
			}
			else if (String.Equals(args.purchasedProduct.definition.id, Coins10000ProductID, StringComparison.Ordinal))
			{
				Debug.Log(string.Format("ProcessPurchase: SUCCESS - Product: '{0}'", args.purchasedProduct.definition.id));
				FightManager.Coins += 10000;
			}

			productToPurchase = null;

			// Return a flag indicating whether this product has completely been received, or if the application needs 
			// to be reminded of this purchase at next app launch. Use PurchaseProcessingResult.Pending when still 
			// saving purchased products to the cloud, and when that save is delayed. 
			return PurchaseProcessingResult.Complete;
		}
			
		public void OnPurchaseFailed(Product product, PurchaseFailureReason failureReason)
		{
			productToPurchase = null;

			// A product purchase attempt did not succeed. Check failureReason for more detail. Consider sharing 
			// this reason with the user to guide their troubleshooting actions.
			Debug.Log(string.Format("OnPurchaseFailed: Product: '{0}', PurchaseFailureReason: {1}", product.definition.id, failureReason));
		}
	}
	#endregion  // callbacks
}