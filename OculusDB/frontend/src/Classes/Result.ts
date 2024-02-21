class ResultData{
  name: string = 'None.';
  id: string = 'null';
  comfortRatingFormatted: string = 'Comfortable for most';
  comfortRating: number = 0;
  categoryFormatted: string = 'Games';
  category: number = 0;
  groupFormatted: string = 'None';
  hasInAppAds: boolean = false;
  inAppLab: boolean = false;
  keywords: Array<string> = [];
  shortDescription: string = 'None.';
  longDescription: string = 'None.';
  priceFormatted: string = '£0';
  offerPriceFormatted: string = '£0';
  priceOffer: boolean = false;
  rawPrice: number = 0;
  priceIsSelected: boolean = true;
  languages: String[] = [];
  
  type: string = 'None';
}

export default ResultData;