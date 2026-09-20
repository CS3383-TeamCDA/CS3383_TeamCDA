# Champion Document — Reusable Prompt Kit

Reverse-engineered from the session that produced `Champion_Document_TL4_Enemy_Combat.docx`. Everything
below is what it actually took to get a correct, complete Champion Document — including the corrections
that only surfaced after the first draft. Attach the same files, paste the master prompt, and this should
land in one pass next time.

---

## 1. Attachments to include in the very first message

| # | File | Why it's needed |
|---|---|---|
| 1 | The sample Champion doc for your course (e.g. `Champion_Sample_Enemy_Feature.docx`) | Defines the visual style to copy: diagram types, box shapes, color use, section formatting. Not the content — just the structure and quality bar. |
| 2 | Your team's AI/architecture project-plan PDF (e.g. `_CS3383_Team_CDA_AIOutput.pdf`) | Contains your feature's *official* spec — Purpose, Responsibilities, Public interface, Internal classes, dynamic binding, GOF pattern, GRASP, Dependencies, Testing strategy, Completion criteria, Effort. This is the source of truth for Section 6, not something to invent. |
| 3 | Your team's **authoritative Context Diagram + Diagram 0** for the whole game | The single most important attachment. Every other DFD you draw must balance against these exactly — same process numbers, same external entities, same data-store IDs. Skipping this is what caused the full DFD rework in this session. |
| 4 | A UML use-case relationship legend (Communicates / Includes / Extends / Generalizes) | Arrow direction and arrowhead style are easy to get wrong (especially Extends) and hard to eyeball-correct without the reference. |
| 5 | (Optional) any reference DFDs already approved by your instructor/team, even for a different feature | Confirms the exact process-box notation (split rounded box: number on top, name below) your grader expects. |

If you don't have #3 yet, say so explicitly in the prompt — see §4 below.

---

## 2. The master prompt (copy-paste, then fill the bracketed parts)

```
Use [SAMPLE_DOC] as the structure and visual-style reference for a Champion Document — match its
diagram types and formatting quality, not its game or content.

My feature is [FEATURE NAME] (TL[N]) for [GAME NAME]. Attached:
1. [SAMPLE_DOC] — structure/style reference only.
2. [PROJECT_PLAN_PDF] — pull my feature's spec from Section 3 (Purpose, Responsibilities, Public
   interface, Internal classes, Class hierarchy/dynamic binding, GOF pattern, GRASP, Dependencies,
   Testing strategy, Completion criteria, Effort) VERBATIM into the "Feature Requirements" section.
   Do not paraphrase it or invent an FR-1/FR-2-style list instead.
3. [TEAM_CONTEXT_DIAGRAM] and [TEAM_DIAGRAM_0] — our team's authoritative DFDs for the whole game.
   Embed these two images EXACTLY as given, unmodified. Every other diagram you draw — especially my
   feature's own zoom — must balance against them: one process per TL member, numbered to match their
   TL number; same external entities; and data-store IDs that do NOT collide with ones already used by
   other features in Diagram 0 (check it first, then start mine at the next free number).
4. [UML_LEGEND] — follow it exactly. In particular: Extends is a dashed arrow whose arrowhead points
   FROM the extending use case TO the base use case (not the other way, and not omitted).

Build a Champion Document with these sections, in this order:

1. Introduction — what the feature does, and explicitly what it does NOT do. For each excluded
   responsibility, name which other TL/feature owns it instead.

2. Use Case — actor(s) + two UML use-case diagrams (per the legend) + two numbered scenarios. Each
   scenario needs: Actors, Preconditions, a numbered Basic sequence, Exceptions that cite the exact
   step they branch from (e.g. "Step 3a"), Postconditions, Priority, and an ID.

3. Interface Contract — three tables: (a) inputs this feature reads from other features, (b) the
   public methods/events this feature exposes, (c) calls this feature makes into other features.
   Every row needs an exact typed signature and a numeric pass/fail test — never a vague description.

4. Data Flow Diagrams — embed the team's Context Diagram + Diagram 0 unmodified (attachments 3 above),
   then zoom into MY one process, decomposed into sub-processes (N.1, N.2, ...). Every input/output on
   my zoom must name its real source/target process or entity exactly as it appears in Diagram 0 (e.g.
   "TakeDamage → Process 6", not a made-up intermediary). Then give a decision-tree process description
   for whichever sub-process is a leaf (not further decomposed).

5. Timeline — a work-items table with honest hour estimates that sum to my actual effort budget from
   the project-plan PDF (attachment 2) — not a round number invented for convenience. Add a PERT
   diagram (top row ES | duration | EF, bottom row LS | slack | LF, critical-path boxes bold/
   highlighted) and a Gantt chart (dark = work, light = slack).

6. Feature Requirements (TL[N] only) — reproduce my feature's spec from attachment 2 verbatim.

7. Design Pattern & Dynamic Binding Analysis —
   - Name the exact classes carrying the required dynamic binding (which base class declares the
     virtual method, which subclasses override it).
   - State explicitly whether a Unity MonoBehaviour lifecycle method (Update, OnCollisionEnter, etc.)
     satisfies that requirement — it does not; that's engine message-dispatch, not the manual C#
     virtual/override dispatch the assignment wants. Explain the two are independent.
   - Name the classes in my GOF pattern and draw a class diagram of it.
   - Compare it honestly against the canonical form at https://sourcemaking.com/design_patterns. If my
     version is a common simplification (e.g. Simple Factory instead of textbook Factory Method with a
     Creator/ConcreteCreator split), say so and justify why — don't force an artificial subclass just
     to match the textbook diagram.

8. Sequence Diagram — at least three classes, modeling one full scenario from Section 2 end to end,
   with proper loop/alt/opt frames around the conditional branches.

9. Class Diagram — every one of my classes with full fields/methods, plus the external classes I
   depend on (dashed border to distinguish them). Every dependency arrow must trace back to a row in
   the Interface Contract — nothing invented.

10. Fine-Grained Time Requirements — break each Section 5 work item into 2-5 subtasks whose hours sum
    back EXACTLY to that item's total. Same budget, finer breakdown for day-to-day tracking.

Formatting: US Letter, match the sample doc's color palette and box styles (e.g. navy borders/text,
a highlight fill for my own processes/classes, a neutral fill for external ones). All diagrams as
embedded images, not just described in prose. Verify the rendered document (convert to PDF, look at
every page) before calling it done.
```

---

## 3. Why these specific phrasings — lessons from this session

Each line below is something the first draft got wrong or had to redo, and the prompt language that
would have prevented it.

- **"Embed these two images EXACTLY as given... every other diagram must balance against them."**
  Without the team's real Diagram 0 up front, I invented my own simplified process numbering (merging
  three teammates' processes into one "Process 5") and my own data-store IDs (D1/D2) — both of which
  collided with the team's real numbering once it was supplied, forcing a rebuild of the zoom diagram,
  the decision tree title, and the sequence diagram's sub-process references.

- **"Data-store IDs that do NOT collide with ones already used... start mine at the next free number."**
  D1–D3 were already claimed by other features in the real Diagram 0. My invented "D1 Enemy Archetypes"
  silently reused an ID that meant something else team-wide.

- **"Extends is a dashed arrow whose arrowhead points FROM the extending case TO the base case."**
  The first pass drew Extends as a plain dashed line with no arrowhead at all — direction and even the
  arrowhead's presence need to be stated explicitly, not assumed from "it's like Includes."

- **"Pull my feature's spec... VERBATIM... do not invent an FR-1/FR-2-style list."**
  Without this, a plausible-sounding but self-authored requirements list gets written instead of using
  the team's actual approved spec — technically defensible, but not what "Feature Requirements" meant
  in context, and not what a grader checking against the team plan would expect to see.

- **"State explicitly whether a MonoBehaviour lifecycle method satisfies [dynamic binding]."**
  This is a genuinely easy trap in Unity assignments: Update()/OnCollisionEnter() look like polymorphism
  but are engine-invoked by name, not C# virtual dispatch. Worth a dedicated line so it doesn't get
  glossed over.

- **"If my version is a simplification... say so and justify why — don't force an artificial subclass."**
  Asked to "compare to sourcemaking.com" without this guardrail, the natural failure mode is either (a)
  claiming false conformance to the textbook pattern, or (b) inventing an unnecessary class hierarchy
  just to match the diagram. Better to state the real design and explain the deliberate deviation.

- **"Sum back EXACTLY to that item's total"** (Section 10) and **"sum to my actual effort budget from
  the project-plan PDF"** (Section 5) — fine-grained/rounded numbers drift from the committed budget
  unless the constraint is stated as a hard arithmetic check, not just "be detailed."

---

## 4. If you don't have the authoritative Diagram 0 yet

Say so directly instead of letting a first draft invent one:

> "I don't yet have my team's official Context Diagram / Diagram 0. Draw a placeholder Diagram 0 with
> one process per feature, numbered 1–[N] to match each TL's number, and flag every process-number and
> data-store ID in it as provisional pending team sign-off — I'll swap in the real diagrams once they
> exist, and everything referencing those numbers should be easy to find and update."

That keeps the numbering scheme consistent with what the real diagram will eventually use (one process
per TL, matching TL numbers) even before it exists, instead of a from-scratch renumbering later.

---

## 5. Final document structure (for reference)

1. Introduction
2. Use Case (2 diagrams + 2 scenarios)
3. Interface Contract (3 tables)
4. Data Flow Diagrams (team Context Diagram + Diagram 0, my zoom, decision tree)
5. Timeline (work items + PERT + Gantt)
6. Feature Requirements (TL-only, verbatim from the project plan)
7. Design Pattern & Dynamic Binding Analysis (+ class diagram of the pattern)
8. Sequence Diagram (≥3 classes)
9. Class Diagram (full feature)
10. Fine-Grained Time Requirements (subtask WBS)
