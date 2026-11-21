// Default character card template. The card data and the localized labels come from cards.json;
// docs/architecture/character-cards.md describes its structure.
#let data = json("cards.json")
#let labels = data.labels

#let card-width = 105mm
#let card-height = 297mm / 4
#let card-padding = 5mm
#let largest-text = 8pt
#let smallest-text = 5pt

#set document(title: labels.documentTitle)
#set page(paper: "a4", margin: 0pt)
#set text(font: "Libertinus Serif", size: largest-text, lang: data.language)
#set par(spacing: 0.6em, leading: 0.45em)

// Shrinks the body, half a point at a time, until it fits; below the smallest size it is cut and ends with "…".
#let fit-to-box(body) = layout(available => {
  let fits(size) = measure(block(width: available.width, text(size: size, body))).height <= available.height
  let sizes = range(int(largest-text.pt() * 2), int(smallest-text.pt() * 2) - 1, step: -1).map(half => half * 0.5pt)
  let size = sizes.find(fits)
  if size != none {
    text(size: size, body)
  } else {
    block(width: available.width, height: available.height, {
      block(height: available.height, clip: true, text(size: smallest-text, body))
      place(bottom + right, box(fill: white, inset: (left: 2pt, top: 1pt), text(size: largest-text)[…]))
    })
  }
})

#let front(card) = {
  set align(center)
  text(size: 6pt)[
    #labels.player #card.playerNumber: #card.playerName
    #h(1fr)
    #labels.character #card.characterNumber
  ]
  v(1fr)
  if card.title != none {
    text(size: 7pt, weight: "bold", upper(card.title))
    linebreak()
  }
  text(size: 12pt, weight: "bold", card.characterName)
  v(1fr)
  let details = (
    (labels.gender, card.gender),
    (labels.species, card.species),
    (labels.type, card.type),
  ).filter(((_, value)) => value != none)
  grid(
    columns: (24mm, 1fr),
    column-gutter: 4mm,
    align: (left + horizon, left + horizon),
    image(bytes(card.qrSvg), format: "svg", width: 24mm),
    fit-to-box(details.map(((label, value)) => [*#label:* #value]).join(linebreak())),
  )
}

#let skill-entry(skill) = {
  let name = if skill.count > 1 [#skill.name ×#skill.count] else [#skill.name]
  if skill.isAdvanced { emph(name) } else { name }
}

#let named-values(entries) = entries.map(entry => [*#entry.name:* #entry.value]).join([ · ])

#let back(card) = fit-to-box({
  [*#labels.experiencePoints:* #card.experiencePoints · *#labels.freePoints:* #card.freePoints]
  if card.isArchived {
    h(1fr)
    text(weight: "bold", upper(labels.archived))
  }
  parbreak()
  if card.validity != none {
    [*#labels.validity:* #card.validity.from – #card.validity.to]
    parbreak()
  }
  if card.cardFlags.len() > 0 {
    named-values(card.cardFlags)
    parbreak()
  }
  if card.stats.len() > 0 {
    named-values(card.stats)
    parbreak()
  }
  if card.talents.len() > 0 {
    [*#upper(labels.skills)*]
    parbreak()
  }
  for talent in card.talents {
    [*#talent.name:* #talent.skills.map(skill-entry).join([, ])]
    parbreak()
  }
})

#let card-cell(body) = block(width: 100%, height: 100%, inset: card-padding, body)

#for (index, page-cards) in data.cards.chunks(4).enumerate() {
  if index > 0 { pagebreak() }
  grid(
    columns: (card-width, card-width),
    rows: card-height,
    stroke: 0.3pt + luma(160),
    ..page-cards.map(card => (card-cell(front(card)), card-cell(back(card)))).flatten(),
  )
}
