# Legacy Web Forms feature specifications

These specifications were derived from the active `.aspx` files and code-behind in `Holonet3\Holonet3`, excluding archived `Backup*` copies. Names in the new product must be franchise-neutral. Existing behavior below is a discovery baseline, not a requirement to reproduce legacy defects or insecure implementation details.

## Web Forms

| Legacy page | Specification |
|---|---|
| `Account\ChangePassword.aspx` | [01](webforms/WEBFORM-01-change-password.md) |
| `Account\ChangePasswordSuccess.aspx` | [02](webforms/WEBFORM-02-password-changed.md) |
| `Account\Login.aspx` | [03](webforms/WEBFORM-03-membership-login.md) |
| `Account\Register.aspx` | [04](webforms/WEBFORM-04-registration.md) |
| `AddOn\QRCodeReader\Default.aspx` | [05](webforms/WEBFORM-05-qr-reader-addon.md) |
| `AnalisiMedica\AnalisiMedica.aspx` | [06](webforms/WEBFORM-06-manual-medical-analysis.md) |
| `AnalisiMedica\NewAnalisiMedica.aspx` | [07](webforms/WEBFORM-07-qr-medical-analysis.md) |
| `CartellaPersonale\FilePersonali.aspx` | [08](webforms/WEBFORM-08-personal-files.md) |
| `Hacking\HackedAccount.aspx` | [09](webforms/WEBFORM-09-hacked-account-menu.md) |
| `Hacking\HackedContacts.aspx` | [10](webforms/WEBFORM-10-hacked-contacts.md) |
| `Hacking\HackedFiles.aspx` | [11](webforms/WEBFORM-11-hacked-files.md) |
| `Hacking\HackedIncoming.aspx` | [12](webforms/WEBFORM-12-hacked-inbox.md) |
| `Hacking\HackedNetwork.aspx` | [13](webforms/WEBFORM-13-hack-network-account.md) |
| `Hacking\HackedNews.aspx` | [14](webforms/WEBFORM-14-hacked-news.md) |
| `Hacking\HackedOutgoing.aspx` | [15](webforms/WEBFORM-15-hacked-outbox.md) |
| `Hacking\NewHackedNetwork.aspx` | [16](webforms/WEBFORM-16-network-account-list.md) |
| `Holodischi\LetturaDischi.aspx` | [17](webforms/WEBFORM-17-manual-datapad-reader.md) |
| `Holodischi\NewLetturaDischi.aspx` | [18](webforms/WEBFORM-18-qr-datapad-reader.md) |
| `IdentificaOggetti\NewIdentificaOggetti.aspx` | [19](webforms/WEBFORM-19-identify-item.md) |
| `IdentificazioneSostanze\NewIdentificazioneSostanze.aspx` | [20](webforms/WEBFORM-20-identify-substance.md) |
| `IdentificazioneTossine\NewIdentificazioneTossine.aspx` | [21](webforms/WEBFORM-21-identify-toxin.md) |
| `Informatica\Informatica.aspx` | [22](webforms/WEBFORM-22-computer-console.md) |
| `LaboratorioBiochimico\NewLaboratorioBiochimico.aspx` | [23](webforms/WEBFORM-23-biochemistry-lab.md) |
| `LaboratorioChimico\NewLaboratorioChimico.aspx` | [24](webforms/WEBFORM-24-chemistry-lab.md) |
| `LaboratorioTecnico\NewCreazioneOggetti.aspx` | [25](webforms/WEBFORM-25-technical-workbench.md) |
| `Login\EndSession.aspx` | [26](webforms/WEBFORM-26-end-session.md) |
| `Login\LoginFPI.aspx` | [27](webforms/WEBFORM-27-faction-login-independent-planets.md) |
| `Login\LoginHolonet.aspx` | [28](webforms/WEBFORM-28-default-network-login.md) |
| `Login\LoginImpero.aspx` | [29](webforms/WEBFORM-29-faction-login-empire.md) |
| `Login\LoginRepubblica.aspx` | [30](webforms/WEBFORM-30-faction-login-republic.md) |
| `Login\NewLogin.aspx` | [31](webforms/WEBFORM-31-qr-login-prototype.md) |
| `Messaggi\Messaggi.aspx` | [32](webforms/WEBFORM-32-messages-legacy.md) |
| `Messaggi\NewMessaggi.aspx` | [33](webforms/WEBFORM-33-messages.md) |
| `Messaggi\NewRubrica.aspx` | [34](webforms/WEBFORM-34-contacts.md) |
| `Messaggi\Rubrica.aspx` | [35](webforms/WEBFORM-35-contacts-legacy.md) |
| `Notizie\Notizie.aspx` | [36](webforms/WEBFORM-36-news-feed.md) |
| `About.aspx` | [37](webforms/WEBFORM-37-about.md) |
| `Default.aspx` | [38](webforms/WEBFORM-38-character-home.md) |
| `SelezioneRete.aspx` | [39](webforms/WEBFORM-39-network-selection.md) |

## Cross-cutting experience specifications

- [QR capture and identity/code scanning](../CROSSCUT-01-qr-capture.md)
- [Theatrical hacking interaction](../CROSSCUT-02-cinematic-hacking.md)
- [Server-authoritative hacking challenges](../CROSSCUT-03-server-authoritative-hacking-challenges.md)

Reusable `.ascx` controls are included as supporting behavior in these page specifications rather than treated as independent Web Forms routes.
