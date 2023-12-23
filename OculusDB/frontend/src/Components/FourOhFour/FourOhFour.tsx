import './FourOhFour.css'

let FourOhFour = () => {
  return (
    <div class="main">
      <div class="info">
        <div class="big-text">404</div>
        <div class="slightly-smaller-text">Page Not Found</div>

        <p style={{ 'text-align': 'center' }}>
          I couldn't find that page... Double check the URL is correct.
        </p>
      </div>
    </div>
  )
}

export default FourOhFour