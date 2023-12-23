class RepoEntryProps{
  time!: Date;
  changelog!: string;
}

let RepoEntry = ( props: RepoEntryProps ) => {
  let changelog = props.changelog;

  let links = [];
  let urlRegex = /https?\:\/\/([^ ()\n\t<>\\]+(\/)?)/g;
  let match = null;

  while((match = urlRegex.exec(changelog)) !== null){
    let replacement = `<a href="${match[0]}" target="_blank">${match[0]}</a>`;

    links.push({
      absolute: replacement,
      relative: match[0],
      start: match.index,
      end: urlRegex.lastIndex
    })
  }

  let length = 0;

  links.forEach(link => {
    changelog = changelog.substring(0, link.start + length) + changelog.substring(link.end +length, changelog.length);
    changelog = [ changelog.slice(0, link.start + length), link.absolute, changelog.slice(link.start + length) ].join('');
    length += link.absolute.length - link.relative.length;
  })

  changelog = changelog.split('\\n').join('<br />');
  changelog = changelog.split('\t').join('&emsp;&emsp;&emsp;&emsp;');

  return (
    <div>
      <b>
        Update on { props.time.getDate() }/{ props.time.getMonth() }/{ props.time.getFullYear() }, { props.time.getHours().toString().padStart(2, '0') }:{ props.time.getMinutes().toString().padStart(2, '0') }:{ props.time.getSeconds().toString().padStart(2, '0') }
      </b><br />

      <p innerHTML={changelog}></p>
    </div>
  )
}

export default RepoEntry