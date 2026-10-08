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
}

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
