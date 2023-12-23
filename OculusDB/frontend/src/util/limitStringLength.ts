let limitStringLength = ( input: string, length: number ): string => {
  if(!input)
    return input;

  return input.length > length ? input.slice(0, length) + '...' : input;
}

export default limitStringLength;