import './Login.css'

let Login = () => {
  let input: HTMLInputElement;
  let submitBtn: HTMLElement;
  let errorBox: HTMLElement;

  if(localStorage.getItem('token')){
    fetch('https://oculusdb-rewrite.rui2015.me/api/v2/login', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({
        password: localStorage.getItem('token')
      })
    })
      .then(data => data.json())
      .then(data => {
        if(data.authorized)
          window.location.href = data.redirect;
      })
  }

  let onClick = () => {
    let value = input.value;

    if(value.includes("@") || value.includes(" ") || value.startsWith("OC")){
      errorBox.innerHTML = '';
      errorBox.appendChild(
        <div>
          Whoops, seems like you wanted to log in with your Oculus/Facebook/Meta Account.<br />
          To do that, visit <a href="https://developer.oculus.com/manage">https://developer.oculus.com/manage</a> and log in there.
        </div> as Node
      )
    }

    fetch('https://oculusdb-rewrite.rui2015.me/api/v2/login', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({
        password: value
      })
    })
      .then(data => data.json())
      .then(data => {
        if(data.authorized){
          localStorage.setItem('token', data.token);
          window.location.href = data.redirect;
        } else{
          errorBox.innerHTML = '';
          errorBox.appendChild(
            <div>
              { data.status }
            </div> as Node
          )
        }
      })
  }

  return (
    <div class="main">
      <div class="info">
        <h2>Dev Login</h2>
        <input class="password-box" type="password" placeholder='Enter Password...' ref={( el ) => input = el} />
        <div class="button" onClick={onClick} ref={( el ) => submitBtn = el}>Submit</div>

        <div ref={( el ) => errorBox = el}></div>
      </div>
    </div>
  )
}

export default Login