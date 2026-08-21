import { environment } from '../env';

const contacts = [
  { label: 'Business', value: 'sanskarin@outlook.in', href: 'mailto:sanskarin@outlook.in' },
  { label: 'Business', value: 'sanskarin.business@gmail.com', href: 'mailto:sanskarin.business@gmail.com' },
  { label: 'Support', value: 'supportramsandesh@gmail.com', href: 'mailto:supportramsandesh@gmail.com' },
];

export function AboutPage() {
  return (
    <section className="page-stack" aria-labelledby="about-title">
      <div className="about-hero panel">
        <img src="/logo.svg" width="88" height="88" alt="" />
        <div>
          <p className="eyebrow">Open-source student management</p>
          <h1 id="about-title">CampusCore</h1>
          <p>A focused Web/PWA for student records, academic operations, staff, communication, administration and reporting.</p>
          <div className="badge-row"><span className="status-pill success">Version {environment.version}</span><span className="status-pill">MIT License</span><span className="status-pill">Made by the Sanskar</span></div>
        </div>
      </div>

      <div className="content-grid two-column">
        <article className="panel">
          <h2>Project links</h2>
          <ul className="link-list">
            <li><a href="https://github.com/sanskarIN/campuscore" target="_blank" rel="noreferrer">CampusCore repository</a><span className="muted">Source, issues, releases and contribution history</span></li>
            <li><a href="https://github.com/sanskarIN" target="_blank" rel="noreferrer">GitHub · sanskarIN</a><span className="muted">More open-source projects</span></li>
            <li><a href="https://buymeacoffee.com/sanskarIN" target="_blank" rel="noreferrer">Buy Me a Coffee</a><span className="muted">Optional support; never required to use CampusCore</span></li>
          </ul>
        </article>
        <article className="panel">
          <h2>Contact & support</h2>
          <ul className="link-list">{contacts.map((contact) => <li key={`${contact.label}-${contact.value}`}><a href={contact.href}>{contact.value}</a><span className="muted">{contact.label}</span></li>)}</ul>
        </article>
      </div>

      <div className="content-grid two-column">
        <article className="panel">
          <h2>Privacy by design</h2>
          <p className="muted">CampusCore keeps authentication tokens in session storage, does not cache authenticated API responses in the service worker, and relies on the server for authorization, validation and audit logging.</p>
        </article>
        <article className="panel">
          <h2>License & contribution</h2>
          <p className="muted">CampusCore is provided under the MIT License. See the repository for contribution, security disclosure, privacy and support documentation.</p>
        </article>
      </div>

      <footer className="about-footer">Made by the Sanskar · CampusCore {environment.version}</footer>
    </section>
  );
}
