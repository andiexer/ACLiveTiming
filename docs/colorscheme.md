Goal:
- Dark, calm, technical
- No neon gamer vibes
- Clear state colors
- Good contrast for long sessions
- Easy to implement as CSS variables

# 🎨 1️⃣ Design Philosophy

This is not:
- ❌ an esports broadcast
- ❌ a flashy sim HUD
- ❌ a marketing website

This is:
- ✅ a functional race control tool
- ✅ readable for hours
- ✅ data-first

So we use:
- Neutral dark base
- One primary accent
- Clear semantic state colors
- Subtle surfaces
- Minimal gradients

# 🎯 2️⃣ Core Color Palette
## Background Layers

### App Background
#0F1221
Very dark blue-gray. Not pure black.

## Primary Surface (panels)

#151936

Used for:
Map panel
Leaderboard container
Selected driver panel
Events panel

## Secondary Surface (hover / active row)

#1C2147

Used for:
Hover row
Selected row background
Active state

## Borders / Dividers

#262B55

Subtle separators.
Never pure lines. Use 1px with low opacity.

# 🔵 3️⃣ Accent Color

We use one main accent color.

## Primary Accent

#3B82F6 (Modern blue)

Used for:
Selected driver left border
Active tab underline
Focus indicators
Map selected highlight
Buttons (primary)

Why blue?
Neutral
Works with red/green state colors
Feels modern & technical

# 🟢 4️⃣ Semantic Colors (State-Based)

These must be consistent everywhere.

## Success (Personal Best, Improvement)

#22C55E

Used for:
PB lap
Negative delta (faster)
Never use bright neon green.

## Warning (Track limits, Pit, Attention)

#F59E0B

Used for:
Track limit
Pit status
Minor warning states

## Danger (Collision, Incident)

#EF4444

Used for:
Collisions
DNF
Severe warnings
Positive delta (slower) if you want red

## Neutral State

#9CA3AF

Used for:
Car names
Secondary labels
Muted info

# ⚪ 5️⃣ Text Hierarchy

Very important for cleanliness.

##Primary Text

#E5E7EB

Driver names
Best lap
Main telemetry values

## Secondary Text

#AAB0D6

Car name
Server info
Minor stats

## Muted Text

#7C83B0

Table labels
Small metadata
Old events

# 📊 6️⃣ Table-Specific Styling
## Header Row Background

#121632

## Table Row Default

Transparent (inherits panel)

## Table Row Hover

#1C2147

## Selected Row
Background: #1C2147
Left border: 3px solid #3B82F6

# 🗺 7️⃣ Map Styling

Track PNG:

Tint white → #C7CBD6

Driver Dots:
Default: #3B82F6
Leader: white border
Selected: outer ring accent blue

No glow.
No gradients.
Keep it technical.

# 📡 8️⃣ Events Panel Colors

Each event type gets a left indicator:

Event	Color
Join	Blue (#3B82F6)
Pit	Amber (#F59E0B)
Track Limit	Amber
Collision	Red (#EF4444)
Session Start	Neutral

Event row hover:
#1C2147


# 🔥 10️⃣ What We Intentionally Avoid

❌ Pink
❌ Neon cyan
❌ Strong gradients
❌ Glassmorphism
❌ Heavy glow effects
❌ Rainbow telemetry

Those make it feel like a Twitch overlay.

You’re building a control tool.

# 🧠 11️⃣ Final Visual Character

If I had to describe the vibe:
Clean
Technical
Motorsport engineer
Slightly modern SaaS
Not gamer RGB

Think:
“Race control dashboard”
Not “Sim racing streamer UI”


/plugin install frontend-design@claude-code-plugins
/plugin marketplace add anthropics/claude-code