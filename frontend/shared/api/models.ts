/**
 * TypeScript mirrors of the backend DTOs (ASP.NET minimal APIs serialize with camelCase web defaults).
 * Grouped by bounded context; kept isolated under core/api so an admin app (Phase 14) can extract them.
 */

// --- Common -------------------------------------------------------------------------------------
export interface Paged<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
  hasPrevious: boolean;
  hasNext: boolean;
}

/** RFC 7807 problem details as returned by the API's ResultHttpExtensions. */
export interface ProblemDetails {
  title?: string;
  detail?: string;
  status?: number;
  code?: string;
  errors?: Record<string, string[]>;
}

// --- Identity -----------------------------------------------------------------------------------
export interface RegisterRequest { email: string; password: string; firstName: string; lastName: string; }
export interface LoginRequest { email: string; password: string; }
export interface AuthTokens {
  accessToken: string;
  tokenType: string;
  accessTokenExpiresAtUtc: string;
  refreshToken: string;
  refreshTokenExpiresAtUtc: string;
}
export interface UserProfile {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
  emailConfirmed: boolean;
  roles: string[];
  permissions: string[];
}

// --- Catalog ------------------------------------------------------------------------------------
export interface ProductListItem {
  id: string;
  name: string;
  slug: string;
  categoryName: string;
  brandName: string | null;
  primaryImageUrl: string | null;
  variantCount: number;
  status: string;
}
export interface ProductVariant {
  id: string; sku: string; name: string; unitPrice: number; currency: string; isActive: boolean;
}
export interface ProductAttribute { name: string; value: string; }
export interface ProductImage { url: string; altText: string | null; sortOrder: number; isPrimary: boolean; }
export interface ProductDetail {
  id: string; name: string; slug: string; description: string; status: string;
  categoryId: string; categoryName: string; brandId: string | null; brandName: string | null;
  variants: ProductVariant[]; attributes: ProductAttribute[]; images: ProductImage[];
}
export interface Category { id: string; name: string; slug: string; description: string | null; parentId: string | null; isActive: boolean; }
export interface Brand { id: string; name: string; slug: string; description: string | null; isActive: boolean; }

// --- Cart ---------------------------------------------------------------------------------------
export interface CartItem {
  id: string; productId: string; variantId: string; sku: string; productName: string;
  unitPrice: number; quantity: number; lineTotal: number;
}
export interface Cart {
  id: string; currency: string; items: CartItem[]; itemCount: number; subtotal: number; expiresAtUtc: string;
}
export interface AddCartItemRequest { variantId: string; quantity: number; }
export interface UpdateCartItemRequest { quantity: number; }

// --- Promotions ---------------------------------------------------------------------------------
export interface CouponQuote { couponCode: string; discountAmount: number; currency: string; }

// --- Orders -------------------------------------------------------------------------------------
export interface ShippingAddress {
  recipientName: string; line1: string; line2: string | null; city: string;
  region: string | null; postalCode: string; country: string;
}
export interface CheckoutRequest { shippingAddress: ShippingAddress; couponCode?: string | null; }
export interface CheckoutResponse { orderId: string; orderNumber: string; status: string; currency: string; grandTotal: number; }
export interface OrderSummary {
  id: string; orderNumber: string; status: string; currency: string; grandTotal: number; itemCount: number; createdAtUtc: string;
}
export interface OrderItem {
  id: string; productId: string; variantId: string; sku: string; productName: string;
  unitPrice: number; quantity: number; lineTotal: number;
}
export interface OrderStatusHistoryEntry { status: string; note: string | null; occurredAtUtc: string; }
export interface OrderDetail {
  id: string; orderNumber: string; customerId: string; status: string; currency: string;
  subtotal: number; discountTotal: number; taxTotal: number; grandTotal: number;
  shippingAddress: ShippingAddress; items: OrderItem[]; history: OrderStatusHistoryEntry[]; createdAtUtc: string;
}
export interface CancelOrderRequest { reason?: string | null; }

// --- Payments -----------------------------------------------------------------------------------
export interface InitiatePaymentRequest { orderId: string; }
export interface PaymentResponse {
  id: string; orderId: string; status: string; amount: number; currency: string;
  providerReference: string | null; failureReason: string | null;
}

// --- Shipping -----------------------------------------------------------------------------------
export interface ShippingMethod { id: string; code: string; name: string; cost: number; currency: string; estimatedDays: number; isActive: boolean; }
export interface ShipmentEvent { status: string; note: string | null; occurredAtUtc: string; }
export interface Tracking { trackingNumber: string; carrier: string; status: string; city: string; country: string; events: ShipmentEvent[]; }

// --- Customers ----------------------------------------------------------------------------------
export interface CustomerProfile {
  id: string; email: string; firstName: string; lastName: string; phone: string | null;
  marketingEmailsOptIn: boolean; preferredLanguage: string;
}
export interface UpdateProfileRequest { firstName: string; lastName: string; phone?: string | null; }
export interface Address {
  id: string; label: string; recipientName: string; line1: string; line2: string | null;
  city: string; region: string | null; postalCode: string; country: string; phone: string | null;
  isDefaultShipping: boolean; isDefaultBilling: boolean;
}
export interface AddressRequest {
  label: string; recipientName: string; line1: string; line2?: string | null; city: string;
  region?: string | null; postalCode: string; country: string; phone?: string | null;
}
export interface WishlistItem { productId: string; addedAtUtc: string; }

// --- Notifications ------------------------------------------------------------------------------
export interface Notification {
  id: string; type: string; title: string; body: string; isRead: boolean; createdAtUtc: string; readAtUtc: string | null;
}
export interface UnreadCount { unread: number; }
export interface MutedChannel { type: string; channel: string; }
export interface NotificationPreferences { muted: MutedChannel[]; }
/** Backend NotificationType/NotificationChannel serialize as numbers on write (web defaults, no string converter). */
export interface UpdateChannelPreferenceRequest { type: number; channel: number; muted: boolean; }

// ================================================================================================
// Admin DTOs (used by the administration app; management endpoints behind permissions)
// ================================================================================================

export interface CreatedResource { id: string; slug?: string; }

// --- Catalog (admin) ----------------------------------------------------------------------------
export interface CreateCategoryRequest { name: string; description?: string | null; parentId?: string | null; }
export interface CreateBrandRequest { name: string; description?: string | null; }
export interface ProductVariantInput { sku: string; name: string; unitPrice: number; currency: string; }
export interface CreateProductRequest {
  name: string; description: string; categoryId: string; brandId?: string | null;
  variants: ProductVariantInput[];
}
export interface AddVariantRequest { sku: string; name: string; unitPrice: number; currency: string; }

// --- Inventory (admin) --------------------------------------------------------------------------
export interface InventoryItem {
  id: string; sku: string; onHand: number; reserved: number; available: number;
  lowStockThreshold: number; isLowStock: boolean;
}
export interface CreateInventoryItemRequest { sku: string; initialOnHand: number; lowStockThreshold: number; }
export interface ReceiveStockRequest { quantity: number; reason?: string | null; }
export interface AdjustStockRequest { delta: number; reason?: string | null; }
export interface SetThresholdRequest { threshold: number; }
export interface StockMovement { id: string; type: string; quantity: number; reason: string | null; reference: string | null; occurredAtUtc: string; }

// --- Promotions (admin) -------------------------------------------------------------------------
export interface Coupon {
  id: string; code: string; name: string; description: string | null; discountType: string;
  discountValue: number; currency: string | null; minimumSubtotal: number; maxDiscountAmount: number | null;
  startsAtUtc: string | null; endsAtUtc: string | null; usageLimit: number | null; perCustomerLimit: number | null;
  timesRedeemed: number; isActive: boolean; createdAtUtc: string;
}
/** discountType: 0 = Percentage, 1 = FixedAmount (backend enum serialized as a number on write). */
export interface CreateCouponRequest {
  code: string; name: string; description?: string | null; discountType: number; discountValue: number;
  currency?: string | null; minimumSubtotal: number; maxDiscountAmount?: number | null;
  startsAtUtc?: string | null; endsAtUtc?: string | null; usageLimit?: number | null; perCustomerLimit?: number | null;
}

// --- Shipping (admin) ---------------------------------------------------------------------------
export interface CreateShippingMethodRequest { code: string; name: string; cost: number; currency: string; estimatedDays: number; }
export interface UpdateShippingMethodRequest { name: string; cost: number; currency: string; estimatedDays: number; }

// --- Notifications (admin) ----------------------------------------------------------------------
/** type: NotificationType enum value (serialized as a number on write). */
export interface SendNotificationRequest {
  recipientId: string; type: number; data?: Record<string, string> | null;
  recipientEmail?: string | null; recipientName?: string | null; idempotencyKey?: string | null;
}
