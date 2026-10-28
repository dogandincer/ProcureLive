export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

export interface AuthResult {
  userId: string;
  email: string;
  firstName: string;
  lastName: string;
  roles: string[];
  companyId: string | null;
  accessToken: string;
  accessTokenExpiresAt: string;
  refreshToken: string;
}

export interface CurrentUser {
  userId: string;
  email: string;
  firstName: string;
  lastName: string;
  roles: string[];
  companyId: string | null;
}

export interface WorkspaceDto {
  id: string;
  name: string;
  description: string | null;
  isActive: boolean;
  companyCount: number;
  productCount: number;
  firmId: string | null;
  firmCode: string | null;
  firmName: string | null;
  isDefault: boolean;
}

export interface SectorDto {
  id: string;
  name: string;
  description: string | null;
  isActive: boolean;
  companyCount: number;
}

export interface CompanyDto {
  id: string;
  /** Cari kodu (CARI-#### otomatik ya da elle). */
  code: string | null;
  name: string;
  taxNumber: string;
  taxOffice: string;
  companyType: string;
  status: string;
  website: string | null;
  workspaceId: string | null;
  workspaceName: string | null;
  averageScore: number | null;
  evaluationCount: number;
  createdAt: string;
  /** Birincil işyeri dışında görünür olduğu işyerleri. */
  visibleWorkspaceNames: string[];
}

export interface CompanyEvaluationDto {
  id: string;
  companyId: string;
  companyName: string;
  qualityScore: number;
  deliveryScore: number;
  communicationScore: number;
  priceScore: number;
  complianceScore: number;
  overallScore: number;
  comments: string | null;
  sourceType: string | null;
  evaluatedAt: string;
}

export interface CompanyLookupResultDto {
  identifier: string;
  identifierType: string;
  isValid: boolean;
  name: string | null;
  tradeName: string | null;
  taxOffice: string | null;
  mersisNo: string | null;
  city: string | null;
  district: string | null;
  addressLine: string | null;
  phone: string | null;
  email: string | null;
  foundationYear: number | null;
  message: string | null;
}

export interface SupplierScoreDto {
  companyId: string;
  companyName: string;
  averageScore: number;
  evaluationCount: number;
  avgQuality: number;
  avgDelivery: number;
  avgCommunication: number;
  avgPrice: number;
  avgCompliance: number;
  sectors: string[];
}

export interface SectorPopularityDto {
  sectorId: string;
  sectorName: string;
  companyCount: number;
  evaluatedCompanyCount: number;
  averageScore: number | null;
}

export interface TopSupplierPerSectorDto {
  sectorId: string;
  sectorName: string;
  companyId: string | null;
  companyName: string | null;
  averageScore: number | null;
  evaluationCount: number;
}

export interface SupplierScoreboardDto {
  topSuppliers: SupplierScoreDto[];
  popularSectors: SectorPopularityDto[];
  topSupplierPerSector: TopSupplierPerSectorDto[];
  totalEvaluatedSuppliers: number;
  totalEvaluations: number;
}

export interface ContactPersonDto {
  id: string;
  firstName: string;
  lastName: string;
  title: string | null;
  email: string;
  phone: string | null;
  isPrimary: boolean;
}

export interface AddressDto {
  id: string;
  addressType: string;
  country: string;
  city: string;
  district: string | null;
  postalCode: string | null;
  line1: string;
  line2: string | null;
}

export interface CompanyDetailDto {
  id: string;
  name: string;
  tradeName: string | null;
  taxNumber: string;
  taxOffice: string;
  mersisNo: string | null;
  companyType: string;
  status: string;
  website: string | null;
  phone: string | null;
  email: string | null;
  foundationYear: number | null;
  employeeCount: number | null;
  description: string | null;
  workspaceId: string | null;
  workspaceName: string | null;
  averageScore: number | null;
  evaluationCount: number;
  createdAt: string;
  contactPersons: ContactPersonDto[];
  addresses: AddressDto[];
  sectors: SectorDto[];
  evaluations: CompanyEvaluationDto[];
  code: string | null;
  visibleWorkspaceIds: string[];
}

export interface ProductImageDto {
  id: string;
  url: string;
  isPrimary: boolean;
  sortOrder: number;
}

export interface ProductDto {
  id: string;
  sku: string;
  name: string;
  description: string | null;
  categoryId: string;
  categoryName: string;
  unitOfMeasure: string;
  brand: string | null;
  isActive: boolean;
  workspaceId: string | null;
  workspaceName: string | null;
  model3dUrl: string | null;
  model3dSource: string | null;
  images: ProductImageDto[];
}

export interface CategoryDto {
  id: string;
  name: string;
  code: string;
  parentCategoryId: string | null;
  children: CategoryDto[];
}

export interface CurrencyDto {
  id: string;
  code: string;
  name: string;
  symbol: string;
  isBaseCurrency: boolean;
}

export interface ExchangeRateDto {
  id: string;
  currencyId: string;
  currencyCode: string;
  rateDate: string;
  rateToBase: number;
  source: string | null;
}

export interface AuditLogDto {
  id: string;
  entityName: string;
  entityId: string;
  action: string;
  changedByUserId: string | null;
  changedByUserName: string | null;
  changedAt: string;
  oldValues: string | null;
  newValues: string | null;
  affectedColumns: string | null;
}

export interface UserDto {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
  roles: string[];
  companyId: string | null;
  isActive: boolean;
  lastLoginAt: string | null;
  /** Art arda 3'ten fazla hatalı şifre nedeniyle otomatik pasife alındığı an. */
  lockedOutAt: string | null;
}

export interface AuctionDto {
  id: string;
  title: string;
  description: string | null;
  auctionType: string;
  status: string;
  ownerCompanyId: string;
  ownerCompanyName: string;
  currencyId: string;
  currencyCode: string;
  startAt: string;
  endAt: string;
  actualStartAt: string | null;
  actualEndAt: string | null;
  reservePrice: number | null;
  extensionRuleMinutes: number;
  itemCount: number;
  invitationCount: number;
  createdAt: string;
  /** Sunucunun verdiği erişim seviyesi; kısıtlı satırlarda hassas alanlar zaten null/0 gelir. */
  accessLevel: AccessLevel;
  isJoined: boolean;
  isListedForPlus: boolean;
  myInvitationStatus: string | null;
  /** Davetsiz ve onaylı cari değil: katılım için talep + ön teklif gerekir. */
  requiresApproval: boolean;
  participationStatus: ParticipationStatus | null;
  /** Yalnızca iç kullanıcı listesinde dolu. */
  workspaceName: string | null;
}

/** Sorumlu satınalmacı adayı: aktif Admin ya da ihalenin işyerine yetkili satınalmacı. */
export interface ResponsibleCandidateDto {
  id: string;
  fullName: string;
  email: string;
  isAdmin: boolean;
}

/** Full: tam erişim · QuotaExceeded: Standart kota doldu (vitrin bilgisi) · NoAccess: içerik yok. */
export type AccessLevel = "Full" | "QuotaExceeded" | "NoAccess";

export interface AuctionItemDto {
  id: string;
  productId: string | null;
  description: string;
  quantity: number;
  unitOfMeasure: string;
  targetPrice: number | null;
  requestedDeliveryDate: string | null;
  specification: string | null;
  sortOrder: number;
  currentBestAmount: number | null;
  bidCount: number;
  /** Tedarikçiye özel: kendi en iyi birim fiyatı, açık ihalede sırası ve teklif veren firma sayısı. */
  myBestAmount?: number | null;
  myRank?: number | null;
  competitorCount?: number | null;
}

export interface AuctionAttachmentDto {
  id: string;
  fileName: string;
  url: string;
  contentType: string | null;
  sizeBytes: number;
}

export interface AuctionInvitationDto {
  id: string;
  companyId: string;
  companyName: string;
  status: string;
  invitedAt: string;
  respondedAt: string | null;
  /** Direct: doğrudan davet · InviteLink: davet linki · SelfJoin: vitrinden kendisi katıldı */
  origin: "Direct" | "InviteLink" | "SelfJoin";
}

export interface AuctionDetailDto {
  id: string;
  title: string;
  referenceCode: string | null;
  description: string | null;
  auctionType: string;
  status: string;
  ownerCompanyId: string;
  ownerCompanyName: string;
  currencyId: string;
  currencyCode: string;
  startAt: string;
  endAt: string;
  actualStartAt: string | null;
  actualEndAt: string | null;
  reservePrice: number | null;
  minDecrementAmount: number | null;
  minDecrementPercentage: number | null;
  extensionRuleMinutes: number;
  extensionDurationMinutes: number | null;
  maxExtensionMinutes: number | null;
  paymentTerm: string | null;
  deliveryTerm: string | null;
  allowAlternativeProducts: boolean;
  allowPartialBids: boolean;
  items: AuctionItemDto[];
  invitations: AuctionInvitationDto[];
  attachments: AuctionAttachmentDto[];
  accessLevel: AccessLevel;
  isJoined: boolean;
  canJoin: boolean;
  isListedForPlus: boolean;
  itemCount: number;
  requiresApproval: boolean;
  myParticipationRequest: ParticipationRequestDto | null;
  /** Yalnızca iç kullanıcıya döner. */
  workspaceId: string | null;
  workspaceName: string | null;
  responsibleUserId: string | null;
  responsibleUserName: string | null;
}

export type ParticipationStatus = "Pending" | "CompanyApproved" | "Accepted" | "Rejected";

export interface ParticipationRequestLineDto {
  auctionItemId: string;
  itemDescription: string;
  quantity: number;
  unitOfMeasure: string;
  unitPrice: number;
  lineTotal: number;
  note: string | null;
  /** null: henüz işlenmedi · true: teklif oldu · false: mevcut en iyiyi geçmediği için atlandı. */
  appliedAsBid: boolean | null;
}

/** Katılım talebi + ön teklif. isCompanyApproved: cari ihalenin işyerinde onaylı mı. */
export interface ParticipationRequestDto {
  id: string;
  auctionId: string;
  auctionTitle: string;
  companyId: string;
  companyName: string;
  companyCode: string | null;
  taxNumber: string;
  companyStatus: string;
  isCompanyApproved: boolean;
  status: ParticipationStatus;
  note: string | null;
  currencyCode: string;
  totalAmount: number;
  createdAt: string;
  companyApprovedAt: string | null;
  decidedAt: string | null;
  rejectReason: string | null;
  lines: ParticipationRequestLineDto[];
  /** Ön teklifin ihale canlıyken ilk teklif olarak işlendiği an (bir kez). */
  preOfferAppliedAt: string | null;
}

/** Sarı alan (LookupField) seçim penceresi satırı: ID döner, Kodu/Adı gösterilir. */
export interface LookupRow {
  id: string;
  code: string;
  name: string;
  detail?: string | null;
}

// --- Firma (işyerlerinin üst ebeveyni) ve marka ---

export interface FirmDto {
  id: string;
  code: string;
  name: string;
  tradeName: string | null;
  shortName: string | null;
  phone: string | null;
  phone2: string | null;
  mobilePhone: string | null;
  fax: string | null;
  email: string | null;
  email2: string | null;
  website: string | null;
  taxOffice: string | null;
  taxNumber: string | null;
  billingCountry: string | null;
  billingCity: string | null;
  billingDistrict: string | null;
  billingPostalCode: string | null;
  billingAddressLine: string | null;
  logoUrl: string | null;
  isDefault: boolean;
  appTitleSuffix: string | null;
  workspaceCount: number;
}

export interface BrandingDto {
  appName: string;
  shortName: string | null;
  suffix: string;
  logoUrl: string | null;
  isCustom: boolean;
}

// --- İhale durum yönetimi ---

export interface StatusTransitionOptionDto {
  to: string;
  requiresReason: boolean;
  requiresEndAt: boolean;
  allowsEndAt: boolean;
  allowsStartAt: boolean;
}

export interface AuctionStatusChangeDto {
  id: string;
  fromStatus: string;
  toStatus: string;
  reason: string | null;
  changedByUserId: string | null;
  changedByName: string | null;
  isAutomatic: boolean;
  changedAt: string;
}

export interface AuctionStatusPanelDto {
  auctionId: string;
  status: string;
  startAt: string;
  endAt: string;
  actualStartAt: string | null;
  actualEndAt: string | null;
  pausedAt: string | null;
  hasAwards: boolean;
  transitions: StatusTransitionOptionDto[];
  history: AuctionStatusChangeDto[];
}

// --- Davet linki (onboarding) ---

export interface InviteLinkDto {
  id: string;
  token: string;
  path: string;
  companyId: string;
  companyName: string;
  email: string | null;
  expiresAt: string;
  usedAt: string | null;
  isExpired: boolean;
  createdAt: string;
}

export interface InviteInfoDto {
  isValid: boolean;
  invalidReason: string | null;
  auctionId: string | null;
  auctionTitle: string | null;
  referenceCode: string | null;
  ownerCompanyName: string | null;
  companyId: string | null;
  companyName: string | null;
  email: string | null;
  expiresAt: string | null;
  auctionEndAt: string | null;
  registrationUsed: boolean;
}

export interface ClaimInviteResultDto {
  auctionId: string;
}

// --- Abonelik / kota ---

export type SeatBand = "Seats5To10" | "Seats10To20" | "Seats20Plus";

export interface SeatBandPriceDto {
  band: SeatBand;
  minUsers: number;
  maxUsers: number | null;
  priceUsd: number;
}

export interface SubscriptionPlansDto {
  bands: SeatBandPriceDto[];
  durationMonths: number;
  standardJoinLimit: number;
}

export interface MySubscriptionDto {
  companyId: string;
  companyName: string;
  plan: "Standard" | "Plus";
  isPlus: boolean;
  seatBand: SeatBand | null;
  priceUsd: number | null;
  expiresAt: string | null;
  joinedCount: number;
  quotaLimit: number;
  remainingJoins: number | null;
  userCount: number;
}

export interface PurchaseResultDto {
  succeeded: boolean;
  paymentReference: string | null;
  failureReason: string | null;
  seatBand: SeatBand | null;
  priceUsd: number | null;
  expiresAt: string | null;
}

// --- Plus arama ---

export interface CompanySearchItemDto {
  id: string;
  name: string;
  tradeName: string | null;
  logoUrl: string | null;
  companyType: string;
  shortAddress: string | null;
  sectors: string[];
  averageScore: number | null;
  /** KVKK: izin yoksa sunucu maskeli döner (te**@***.com), website hiç gelmez. */
  email: string | null;
  phone: string | null;
  website: string | null;
  contactMasked: boolean;
  openTenderCount: number;
}

export interface TenderSearchItemDto {
  id: string;
  title: string;
  referenceCode: string | null;
  auctionType: string;
  status: string;
  ownerCompanyId: string;
  ownerCompanyName: string;
  ownerLogoUrl: string | null;
  startAt: string;
  endAt: string;
  itemCount: number;
  participantCount: number;
  isJoined: boolean;
}

export interface ProductSearchItemDto {
  id: string;
  sku: string;
  name: string;
  categoryName: string;
  unitOfMeasure: string;
  thumbnailUrl: string | null;
  openTenderCount: number;
}

export interface SearchResultDto {
  companies: CompanySearchItemDto[];
  tenders: TenderSearchItemDto[];
  products: ProductSearchItemDto[];
}

// --- Firma ayarları ---

export interface CompanySettingsDto {
  companyId: string;
  companyName: string;
  companyType: string;
  logoUrl: string | null;
  showContactInfo: boolean;
  listTendersForPlus: boolean;
  isPlus: boolean;
  plusExpiresAt: string | null;
}

export interface BidDto {
  id: string;
  auctionItemId: string;
  companyId: string;
  companyName: string;
  amount: number;
  currencyId: string;
  bidTime: string;
  status: string;
  isMine: boolean;
}

export interface BidComparisonDto {
  bidId: string;
  companyId: string;
  companyName: string;
  amount: number;
  currencyCode: string;
  normalizedAmountInBaseCurrency: number;
  bidTime: string;
  status: string;
  /** Sunucudaki ödül kaydı: kazanan seçildi mi (canlıdaki "Winning" durumu ödül değildir). */
  isAwarded: boolean;
}

export interface AuctionFeedEntryDto {
  bidId: string;
  auctionItemId: string;
  itemDescription: string;
  companyName: string | null;
  amount: number | null;
  currencyCode: string;
  isMine: boolean;
  bidTime: string;
}

export interface NotificationDto {
  id: string;
  type: string;
  title: string;
  body: string;
  relatedEntityType: string | null;
  relatedEntityId: string | null;
  isRead: boolean;
  readAt: string | null;
  createdAt: string;
}

export interface RfiTemplateDto {
  id: string;
  name: string;
  description: string | null;
  categoryId: string | null;
  isActive: boolean;
  questions: RfiQuestionDto[];
}

export interface RfiQuestionDto {
  id: string;
  questionText: string;
  questionType: string;
  isRequired: boolean;
  sortOrder: number;
  weight: number;
  options: RfiQuestionOptionDto[];
}

export interface RfiQuestionOptionDto {
  id: string;
  optionText: string;
  scoreValue: number;
}

export interface RfiProcessDto {
  id: string;
  rfiTemplateId: string;
  templateName: string;
  title: string;
  ownerCompanyId: string;
  ownerCompanyName: string;
  status: string;
  dueDate: string;
  invitationCount: number;
  responseCount: number;
  createdAt: string;
  /** Tedarikçi: Standart kota dolu ve davet kabul edilmemişse QuotaExceeded ("Plus alınız"). */
  accessLevel: AccessLevel;
  myInvitationStatus: string | null;
  /** Yalnızca iç kullanıcıya dolu. */
  workspaceName: string | null;
}

export interface RfiMyAnswerDto {
  rfiQuestionId: string;
  answerText: string | null;
  answerNumber: number | null;
  answerBool: boolean | null;
  selectedOptionId: string | null;
  attachmentUrl?: string | null;
}

export interface RfiProcessDetailDto {
  id: string;
  rfiTemplateId: string;
  templateName: string;
  title: string;
  ownerCompanyId: string;
  ownerCompanyName: string;
  status: string;
  dueDate: string;
  questions: RfiQuestionDto[];
  invitations: AuctionInvitationDto[];
  accessLevel: AccessLevel;
  /** Süreç yanıtlara açık mı (Gönderildi / Devam Ediyor). */
  isOpen: boolean;
  hasSubmitted: boolean;
  /** İlk yanıt her üyeye; gönderilmiş yanıtın revizyonu yalnızca Plus'a ve açık süreçte. */
  canSubmit: boolean;
  myAnswers: RfiMyAnswerDto[] | null;
  /** Yalnızca iç kullanıcıya dolu. */
  workspaceId: string | null;
  workspaceName: string | null;
}

export interface RfiResponseDto {
  rfiQuestionId: string;
  questionText: string;
  companyId: string;
  companyName: string;
  answerText: string | null;
  answerNumber: number | null;
  answerBool: boolean | null;
  selectedOptionText: string | null;
  attachmentUrl: string | null;
}

export interface SupplierEvaluationDto {
  id: string;
  companyId: string;
  companyName: string;
  totalScore: number;
  comments: string | null;
  evaluatedAt: string;
}

export interface RfqProcessDto {
  id: string;
  title: string;
  ownerCompanyId: string;
  ownerCompanyName: string;
  currencyId: string;
  currencyCode: string;
  status: string;
  dueDate: string;
  lineItemCount: number;
  invitationCount: number;
  quoteCount: number;
  createdAt: string;
  /** Tedarikçi: Standart kota dolu ve davet kabul edilmemişse QuotaExceeded ("Plus alınız"). */
  accessLevel: AccessLevel;
  myInvitationStatus: string | null;
  /** Yalnızca iç kullanıcıya dolu. */
  workspaceName: string | null;
}

export interface RfqLineItemDto {
  id: string;
  productId: string | null;
  description: string;
  quantity: number;
  unitOfMeasure: string;
  sortOrder: number;
}

export interface RfqProcessDetailDto {
  id: string;
  title: string;
  ownerCompanyId: string;
  ownerCompanyName: string;
  currencyId: string;
  currencyCode: string;
  status: string;
  dueDate: string;
  winningQuoteId: string | null;
  lineItems: RfqLineItemDto[];
  invitations: AuctionInvitationDto[];
  accessLevel: AccessLevel;
  /** Süreç yanıtlara açık mı (Gönderildi / Devam Ediyor). */
  isOpen: boolean;
  hasSubmitted: boolean;
  /** İlk yanıt her üyeye; gönderilmiş yanıtın revizyonu yalnızca Plus'a ve açık süreçte. */
  canSubmit: boolean;
  /** Yalnızca iç kullanıcıya dolu. */
  workspaceId: string | null;
  workspaceName: string | null;
}

export interface RfqQuoteLineItemDto {
  rfqLineItemId: string;
  lineItemDescription: string;
  unitPrice: number;
  currencyId: string;
  currencyCode: string;
  normalizedUnitPriceInBaseCurrency: number;
  leadTimeDays: number | null;
  notes: string | null;
}

export interface RfqQuoteDto {
  id: string;
  companyId: string;
  companyName: string;
  status: string;
  submittedAt: string | null;
  validUntil: string | null;
  notes: string | null;
  lineItems: RfqQuoteLineItemDto[];
}

export interface RfqComparisonRowDto {
  rfqLineItemId: string;
  lineItemDescription: string;
  companyId: string;
  companyName: string;
  unitPrice: number;
  currencyCode: string;
  normalizedUnitPriceInBaseCurrency: number;
  leadTimeDays: number | null;
}

export interface MonitoringStatsDto {
  auctionCountByStatus: Record<string, number>;
  rfqCountByStatus: Record<string, number>;
  totalBidsToday: number;
  totalBidsThisWeek: number;
  liveAuctions: LiveAuctionSummaryDto[];
  recentBids: RecentBidDto[];
}

export interface LiveAuctionSummaryDto {
  auctionId: string;
  title: string;
  bidCount: number;
  bestBidAmount: number | null;
  bestBidCurrencyCode: string | null;
  leadingCompanyName: string | null;
  endsAt: string;
}

export interface RecentBidDto {
  bidId: string;
  auctionTitle: string;
  companyName: string;
  amount: number;
  currencyCode: string;
  bidTime: string;
}

export interface RfqAiRecommendationDto {
  recommendedQuoteId: string;
  recommendedCompanyName: string;
  confidenceScore: number;
  rationale: string;
  scores: RfqQuoteScoreDto[];
}

export interface RfqQuoteScoreDto {
  quoteId: string;
  companyName: string;
  totalScore: number;
  priceScore: number;
  leadTimeScore: number;
  totalNormalizedCost: number;
  averageLeadTimeDays: number | null;
}

export interface AuctionModelRecommendationDto {
  recommendedAuctionType: string;
  confidenceScore: number;
  rationale: string;
  featuresUsed: Record<string, unknown>;
}
