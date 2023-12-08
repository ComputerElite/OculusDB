import Application from "../../../Classes/Application"

class ResultProps{
  app!: Application
}

let Result = ( props: ResultProps ) => {
  return (
    <div class="result">
      <div class="result-branding">
        <div class="result-"></div>
        { props.app.name }
      </div>
    </div>
  )
}

export default Result