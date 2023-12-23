import './Search.css'

class SearchProps{
  value?: () => string;
  setCurrentTab!: ( tab: string ) => string;
}

let Search = ( props: SearchProps ) => {
  return (
    <div class="search-bar">
      <input placeholder='Search...' value={ props.value ? props.value() : '' } onChange={( el ) => el.currentTarget.value !== '' ? props.setCurrentTab('/search/' + el.currentTarget.value) : null } />
      <i class="fa-solid fa-magnifying-glass"></i>
    </div>
  )
}

export default Search;