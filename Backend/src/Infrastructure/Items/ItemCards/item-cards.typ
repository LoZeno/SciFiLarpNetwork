// Default item card template. The card data and the localized labels come from cards.json;
// docs/architecture/item-cards.md describes its structure.
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
  text(size: 7pt, upper(card.itemType))
  v(2mm)
  text(size: 12pt, weight: "bold", card.name)
  v(1fr)
  image(bytes(card.qrSvg), format: "svg", width: 30mm)
  v(1fr)
}

#let back(card) = fit-to-box({
  if card.description != none {
    [*#labels.description:* #card.description]
    parbreak()
  }
  if card.effect != none {
    [*#labels.effect:* #card.effect]
    parbreak()
  }
  if card.charges != none {
    [*#labels.charges:* #card.charges]
    parbreak()
  }
  if card.expiresOn != none {
    [*#labels.expires:* #card.expiresOn]
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
