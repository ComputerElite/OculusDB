import { For, Show, onMount } from 'solid-js';
import jsonview from '@pgrabovets/json-view';
import './APIDocs.css'

let APIDocs = () => {
  let contents: HTMLElement;
  let responseElementList: Array<HTMLElement> = [];

  onMount(() => {
    fetch('https://oculusdb-rewrite.rui2015.me/api/api.json')
      .then(data => data.json())
      .then((data) => {
        contents.innerHTML = '';
        contents.appendChild((
          <div>
            <h1>Enums</h1>

            <For each={data.enums}>
              {( item, index ) =>  <>
                <style>
                  {`#enum-dropdown-${index()}:checked ~ .dropdown{
                    height: fit-content;
                  }

                  #enum-dropdown-${index()}:checked ~ .dropdown > .dropdown-contents-enum${index()}{
                    display: block;
                    height: fit-content;
                    opacity: 1;
                  }

                  #enum-dropdown-${index()}:checked ~ .dropdown .dropdown-heading-enum${index()} i{
                    rotate: 90deg;
                  }`}
                </style>

                <input type="checkbox" id={"enum-dropdown-"+index()} style={{ display: 'none' }} />
                <div class="dropdown">
                  <label for={"enum-dropdown-"+index()}>
                    <div class={"dropdown-heading dropdown-heading-enum"+index()}>
                      <p>
                        <i class="fa-solid fa-circle-arrow-right"></i> { item.name }
                      </p>
                    </div>
                  </label>

                  <div class={"dropdown-contents dropdown-contents-enum"+index()}>
                    { item.description }

                    <h2>Values</h2>

                    <For each={item.values}>
                      {( value ) =>
                        <p style={{ width: 'fit-content', display: 'inline-block', margin: '20px' }}>
                          <b>{ value.enumName }</b><br />
                          <span style={{ color: '#aaa' }}>Value:</span> <b>{ value.value }</b><br />
                          { value.description }
                        </p>
                      }
                    </For>
                  </div>
                </div>
                <br />
              </> }
            </For>

            <br />
            <h1>Endpoints</h1>

            <For each={data.endpoints}>
              {( item, index ) =>  <>
                <style>
                  {`#endpoint-dropdown-${index()}:checked ~ .dropdown{
                    height: fit-content;
                  }

                  #endpoint-dropdown-${index()}:checked ~ .dropdown > .dropdown-contents-endpoint${index()}{
                    display: block;
                    height: fit-content;
                    opacity: 1;
                  }

                  #endpoint-dropdown-${index()}:checked ~ .dropdown .dropdown-heading-endpoint${index()} i{
                    rotate: 90deg;
                  }`}
                </style>

                <input type="checkbox" id={"endpoint-dropdown-"+index()} style={{ display: 'none' }} />
                <div class="dropdown">
                  <label for={"endpoint-dropdown-"+index()}>
                    <div class={"dropdown-heading dropdown-heading-endpoint"+index()}>
                      <p>
                        <i class="fa-solid fa-circle-arrow-right"></i> { item.method } { item.url }
                      </p>
                    </div>
                  </label>

                  <div class={"dropdown-contents dropdown-contents-endpoint"+index()}>
                    { item.description }

                    <h2>Parameters</h2>

                    <For each={item.parameters}>
                      {( value ) =>
                        <p style={{ width: 'fit-content', display: 'inline-block', margin: '20px' }}>
                          <b>{ value.name }</b><br />
                          { value.description }<br />

                          <Show when={value.required}>
                            <i class="fa-solid fa-check" style={{ color: '#0f0' }}></i> Required
                          </Show>

                          <Show when={!value.required}>
                            <i class="fa-solid fa-xmark" style={{ color: '#f00' }}></i> Not Required
                          </Show>
                        </p>
                      }
                    </For>

                    <h2>Example Request</h2>
                    <p>{ item.method } { item.url }</p>

                    <div ref={( el ) => responseElementList.push(el)}></div>
                  </div>
                </div>
                <br />
              </> }
            </For>
          </div>
        ) as Node);

        data.endpoints.forEach(( item: any, i: number ) => {
          try{
            jsonview.renderJSON(JSON.stringify(item.exampleResponse), responseElementList[i]);
          } catch(e){
            responseElementList[i].appendChild((
              <div class="json-container">{ item.exampleResponse }</div>
            ) as Node);
          }
        })
      });
  })

  return (
    <div class="main">
      <div class="info" ref={( el ) => contents = el} style={{ "text-align": 'center' }}>Loading...</div>
    </div>
  )
}

export default APIDocs