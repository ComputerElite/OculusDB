let limitStringLength = ( input: string, length: number ): string => {
  return input.length > length ? input.slice(0, length) + '...' : input;
}

export default limitStringLength;