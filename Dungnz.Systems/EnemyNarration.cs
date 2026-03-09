namespace Dungnz.Systems;

/// <summary>
/// Provides per-enemy encounter introduction and death narration line pools.
/// Note: DungeonBoss subclasses are routed to <see cref="BossNarration"/> by CombatEngine
/// and do not use this class.
/// </summary>
public static class EnemyNarration
{
    private static readonly Dictionary<string, string[]> _intros = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Goblin"] = new[]
        {
            "A snarling goblin leaps from the shadows, blade glinting!",
            "A filthy goblin scrambles toward you, cackling with glee!",
            "A goblin darts out from behind a pillar, teeth bared!"
        },
        ["Skeleton"] = new[]
        {
            "Bones clatter as a skeleton lurches forward, hollow eyes fixed on you!",
            "A skeleton rises from the dust, jaw snapping with dry malice!",
            "The skeleton advances, sword arm raised, death rattling with every step!"
        },
        ["Troll"] = new[]
        {
            "The ground shakes as a massive troll heaves into view, knuckles dragging stone!",
            "A foul-smelling troll rounds the corner and lets out a thunderous roar!",
            "A troll slams its fists together with a boom that echoes through the dungeon!"
        },
        ["Dark Knight"] = new[]
        {
            "Armour black as midnight, a Dark Knight charges forward with lethal purpose!",
            "A Dark Knight steps from the darkness, visor down, blade levelled at your throat!",
            "The clink of dark steel announces the Dark Knight before you see it — too late to run!"
        },
        ["Goblin Shaman"] = new[]
        {
            "A hunched Goblin Shaman cackles, arcane sparks dancing between its claws!",
            "The Goblin Shaman raises a gnarled staff, chanting in a guttural tongue!",
            "Reeking of sulfur, a Goblin Shaman eyes you and begins weaving a dark hex!"
        },
        ["Stone Golem"] = new[]
        {
            "With a grinding groan the Stone Golem lurches to life, its gaze empty and inevitable!",
            "Boulders scrape together as the Stone Golem assembles itself in your path!",
            "A Stone Golem rises from the rubble, fists raised like battering rams!"
        },
        ["Wraith"] = new[]
        {
            "A cold shriek tears the air as the Wraith phases through the wall toward you!",
            "The torches snuff out — a Wraith drifts from the dark, trailing shadow!",
            "The temperature plummets as a Wraith coalesces before you, hungry and hateful!"
        },
        ["Vampire Lord"] = new[]
        {
            "A Vampire Lord descends from the vaulted ceiling, cape unfurling like wings!",
            "Red eyes open in the dark — the Vampire Lord smiles and reveals its fangs!",
            "The Vampire Lord glides forward, voice like silk: 'Your blood smells… exquisite.'"
        },
        ["Mimic"] = new[]
        {
            "The treasure chest you were reaching for SNAPS open — it's a Mimic!",
            "What you took for a pile of gold unfurls, rows of teeth gleaming — a Mimic!",
            "The innocent-looking chest suddenly sprouts legs and lunges — Mimic!",
        }
    };

    private static readonly Dictionary<string, string[]> _deaths = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Goblin"] = new[]
        {
            "The goblin crumples with a pitiful shriek.",
            "The goblin collapses, its foul laughter silenced.",
            "The goblin slumps to the ground, twitching."
        },
        ["Skeleton"] = new[]
        {
            "The skeleton shatters into a heap of rattling bones.",
            "With a dry crack the skeleton falls, the light leaving its empty sockets.",
            "The skeleton collapses, its bones clattering across the dungeon floor."
        },
        ["Troll"] = new[]
        {
            "The troll topples like a felled oak, shaking the floor on impact.",
            "The troll lets out a final, confused grunt and crashes down.",
            "The massive troll slumps, its regeneration finally overwhelmed."
        },
        ["Dark Knight"] = new[]
        {
            "The Dark Knight staggers, then buckles — armour clanging as it hits the stone.",
            "The Dark Knight falls to one knee and is still, shadow fading from its visor.",
            "With a hollow groan the Dark Knight crumples, black steel ringing on the floor."
        },
        ["Goblin Shaman"] = new[]
        {
            "The Goblin Shaman's hex fizzles out as it topples, staff clattering away.",
            "The Goblin Shaman shrieks once, then crumples in a heap of robes and bones.",
            "With its last breath the Goblin Shaman curses you — then goes limp."
        },
        ["Stone Golem"] = new[]
        {
            "The Stone Golem groans, cracks spreading across its body, then shatters into gravel.",
            "The Stone Golem freezes mid-swing, crumbles, and collapses into rubble.",
            "With a grinding roar the Stone Golem breaks apart, chunks of rock raining down."
        },
        ["Wraith"] = new[]
        {
            "The Wraith releases a final banshee wail and dissolves into cold mist.",
            "The Wraith flickers, screams, and is torn apart by its own darkness.",
            "Light floods back as the Wraith unravels, its shriek fading into silence."
        },
        ["Vampire Lord"] = new[]
        {
            "The Vampire Lord recoils, then bursts into a cloud of ash and crimson mist.",
            "The Vampire Lord hisses, 'Impossible…' and crumbles to dust before you.",
            "The Vampire Lord's eyes go dark and it collapses, its ancient body dissolving."
        },
        ["Mimic"] = new[]
        {
            "The Mimic's disguise falls apart as it expires — just a pile of teeth and wood.",
            "The Mimic snaps once more, weakly, then lies still — its treasure finally real.",
            "The Mimic shudders, its lid slamming shut forever."
        },
        ["Giant Rat"] = new[]
        {
            "The Giant Rat convulses and is still, its plague-eyes glazed.",
            "The Giant Rat crumples mid-stride, momentum spent, finally quiet.",
            "A final, shrill squeak — then nothing. The Giant Rat doesn't get up."
        },
        ["Cursed Zombie"] = new[]
        {
            "The Cursed Zombie sags mid-step and does not rise again.",
            "The curse holding it together unravels. It collapses in sections.",
            "Whatever was driving the Cursed Zombie forward goes out of it all at once."
        },
        ["Blood Hound"] = new[]
        {
            "The Blood Hound collapses mid-stride, the hunt ending where it stood.",
            "A short whimper, then the Blood Hound goes down, the scent of you its last sensation.",
            "The Blood Hound skids across the stone and lies still. The chase is over."
        },
        ["Iron Guard"] = new[]
        {
            "The Iron Guard buckles, discipline failing, and hits the floor with a tremendous crash.",
            "With a groan of stressed metal, the Iron Guard folds and goes down.",
            "The Iron Guard's spear clatters first. Then the Iron Guard follows."
        },
        ["Night Stalker"] = new[]
        {
            "The Night Stalker crumples from somewhere just behind you, its silence finally permanent.",
            "It drops without a sound — as it lived, so it ends.",
            "The Night Stalker fades from the shadows into them, and does not return."
        },
        ["Frost Wyvern"] = new[]
        {
            "The Frost Wyvern falls and does not melt. It simply goes very still.",
            "Ice cracks from the Frost Wyvern's body as it collapses, the cold bleeding out of it.",
            "The Frost Wyvern crashes down, wings spread, a monument of ice and silence."
        },
        ["Chaos Knight"] = new[]
        {
            "The Chaos Knight's momentum fails — it stumbles, and reality snaps back as it falls.",
            "The distortions collapse inward as the Chaos Knight drops. The air stills.",
            "The Chaos Knight hits the floor and the chaos goes with it. Silence rushes in."
        },
        ["Lich King"] = new[]
        {
            "The Lich King simply stops. No final words. No dramatics. It ceases.",
            "The crown hits the stone first. Then the Lich King crumbles into it, piece by piece.",
            "Centuries of power unravel in a single moment. What remains barely constitutes remains."
        },
        ["Shadow Imp"] = new[]
        {
            "The Shadow Imps scatter, shrieking, and blink out one by one in small pops of darkness.",
            "The swarm breaks apart — a chorus of tiny screams, then nothing.",
            "The shadows they came from reclaim them. The room is quiet again."
        },
        ["Carrion Crawler"] = new[]
        {
            "The Carrion Crawler twitches, legs curling inward, its venom pooling beneath it.",
            "With a wet, diminishing chittering, the Carrion Crawler rolls onto its back and goes still.",
            "The Carrion Crawler collapses mid-lunge and deflates. The smell lingers."
        },
        ["Dark Sorcerer"] = new[]
        {
            "The Dark Sorcerer's spell dies with it — a flash, a crack, then silence.",
            "The Dark Sorcerer drops with the same absence of expression it lived with.",
            "The power winked out of it like a candle. It falls, and the air is ordinary again."
        },
        ["Bone Archer"] = new[]
        {
            "The Bone Archer's bow drops first — then the skeleton, arrows scattering across the stone.",
            "A last arrow goes wide as the Bone Archer falls, its aim finally failing.",
            "The Bone Archer shatters backward, ribs first, and the quiver empties around it."
        },
        ["Crypt Priest"] = new[]
        {
            "The Crypt Priest's incantation breaks mid-word. It folds silently to the stone.",
            "The runes orbiting its skull extinguish one by one. The Crypt Priest follows.",
            "Its final prayer goes unanswered. The Crypt Priest slumps forward and is still."
        },
        ["Plague Bear"] = new[]
        {
            "The Plague Bear crashes down, shaking the floor, its growl fading into a wet rattle.",
            "It sways once, enormous, then the Plague Bear falls — disease and all.",
            "The Plague Bear's roar becomes a sigh and then stops. The dungeon is quieter for it."
        },
        ["Siege Ogre"] = new[]
        {
            "The Siege Ogre topples like something architectural. The dungeon takes a moment to settle.",
            "It falls in stages — first the knees, then everything else. The floor doesn't forgive it.",
            "The Siege Ogre crashes down with the sound of a building deciding to give up."
        },
        ["Blade Dancer"] = new[]
        {
            "The Blade Dancer's final spin ends awkwardly. The grace, at last, is gone.",
            "A blade clatters wide and the Blade Dancer goes down mid-movement, the dance cut short.",
            "The Blade Dancer stills. The performance is over."
        },
        ["Mana Leech"] = new[]
        {
            "The Mana Leech shudders as the stolen energy unravels inside it — then goes flat.",
            "The Mana Leech releases its hold and drops, the hunger finally extinguished.",
            "It deflates, slowly, like something that no longer has anything to hold it together."
        },
        ["Shield Breaker"] = new[]
        {
            "The Shield Breaker drops its weapon first, then follows it. All that force, suddenly inert.",
            "The Shield Breaker hits the floor with nothing left in it.",
            "It falls like a wall coming down — inevitable, complete, final."
        },
        ["Archlich Sovereign"] = new[]
        {
            "The Archlich Sovereign's crown clatters to the floor. The dead drop with it.",
            "The sovereignty ends. The Archlich Sovereign crumbles, and the centuries leave it.",
            "It stills — not with a bang, but with the quiet finality of a dynasty ending."
        },
        ["Abyssal Leviathan"] = new[]
        {
            "The Abyssal Leviathan rolls slowly, impossibly large, and goes still. The pressure drops.",
            "A deep, shuddering exhalation — then the Abyssal Leviathan settles into the floor and moves no more.",
            "The darkness that followed it recedes. The Abyssal Leviathan does not."
        },
        ["Infernal Dragon"] = new[]
        {
            "The Infernal Dragon's fire gutters out. It collapses with a sound like a furnace dying.",
            "The flames die first. Then the Infernal Dragon follows, smoke rising from the stone.",
            "Heat leaves the room all at once as the Infernal Dragon crashes down. The quiet is absolute."
        }
    };

    private static readonly Dictionary<string, string[]> _critReactions = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Goblin"] = new[]
        {
            "HEHEHE! Didn't see that coming, did ya?!",
            "Lucky shot? No. I'm just better than you.",
            "That one's gonna leave a mark!"
        },
        ["Skeleton"] = new[]
        {
            "Foolish mortal. Your bones will join mine.",
            "The curse grows stronger with each wound.",
            "Death finds you, whether you like it or not."
        },
        ["Troll"] = new[]
        {
            "Your weakness pleases me. Feel the forest's wrath!",
            "Smash! Again and again until you BREAK!",
            "Stone and muscle — you have neither."
        },
        ["Dark Knight"] = new[]
        {
            "Pathetic. I've cleaved kingdoms apart.",
            "Your end comes swiftly, little worm.",
            "The darkness hungers, and I am its instrument."
        },
        ["Malachar the Undying"] = new[]
        {
            "Your defenses crumble before eternity's hunger.",
            "I have fed on stronger souls than you.",
            "Despair, mortal. I cannot be stopped."
        },
        ["Goblin Shaman"] = new[]
        {
            "The hex deepens! Your doom is WRITTEN!",
            "Dark magic flows through my claws!",
            "Feel the pain of a thousand curses!"
        },
        ["Stone Golem"] = new[]
        {
            "Granite meets flesh. There is no contest.",
            "My fist descends like an avalanche.",
            "Unmovable. Unstoppable. Inevitable."
        },
        ["Wraith"] = new[]
        {
            "Your life force splinters beneath my touch!",
            "The void hungers and feeds on your suffering.",
            "Scream — none will hear you in the shadow."
        },
        ["Vampire Lord"] = new[]
        {
            "Exquisite! Your blood sings to me.",
            "Centuries of hunger made me unstoppable.",
            "I have bathed in the lifeblood of empires."
        },
        ["Mimic"] = new[]
        {
            "You thought it treasure. It WAS your doom!",
            "My hunger runs deeper than any gold.",
            "Swallow your greed whole!"
        },
        ["Giant Rat"] = new[]
        {
            "SQUEAK! Your flesh tastes EXQUISITE!",
            "The plague runs through you now!",
            "My teeth find every weakness!"
        },
        ["Cursed Zombie"] = new[]
        {
            "The curse SPREADS through you like rot!",
            "Your end has been written since before birth.",
            "Death walks. Death festers. Death WINS."
        },
        ["Blood Hound"] = new[]
        {
            "Your blood sings to me like a siren's call!",
            "I taste your weakness, your FEAR!",
            "Pack hunter. Lone prey. How predictable."
        },
        ["Iron Guard"] = new[]
        {
            "Steel discipline meets your reckless thrashing.",
            "Ten thousand soldiers stand in me. You stand alone.",
            "Form. Discipline. Victory."
        },
        ["Night Stalker"] = new[]
        {
            "From shadow I strike. You never saw it coming.",
            "Darkness is my ally. You have none.",
            "Every moment you live is borrowed."
        },
        ["Frost Wyvern"] = new[]
        {
            "Ice crystallizes in your veins. Surrender to cold.",
            "Centuries of winter made me merciless.",
            "Your warmth fades. The frost takes all."
        },
        ["Chaos Knight"] = new[]
        {
            "Entropy bends to my will. You are CHAOS FOOD!",
            "Reality shatters where I tread.",
            "Order dies screaming. Your scream is next."
        },
        ["Lich King"] = new[]
        {
            "Millennia of dark magic crackle in my touch.",
            "You were dust long before you were born.",
            "Crown me with your suffering, mortal."
        },
        ["Shadow Imp"] = new[]
        {
            "The shadows MULTIPLY! We are LEGION!",
            "Many as shadows. Quick as thought. Merciless.",
            "You twitch while WE feast!"
        },
        ["Carrion Crawler"] = new[]
        {
            "Poison courses through me — through YOU now!",
            "Decay spreads at my touch. Resist it. You CAN'T!",
            "The swarm devours all."
        },
        ["Dark Sorcerer"] = new[]
        {
            "Arcane forces rend your flesh asunder!",
            "Magic bends to my will alone.",
            "You are a candle. I am the abyss."
        },
        ["Bone Archer"] = new[]
        {
            "From the shadows, death finds its mark AGAIN.",
            "My arrows never miss. Your luck has run out.",
            "One shot. Precisely placed. Your end."
        },
        ["Crypt Priest"] = new[]
        {
            "Prayer cannot save you from the JUDGMENT I deliver!",
            "Death is divine, and I am its vessel.",
            "Repent. It changes nothing."
        },
        ["Plague Bear"] = new[]
        {
            "Disease ravages your body with EVERY wound!",
            "The rot spreads. Soon there's nothing left.",
            "I am pestilence given flesh and rage."
        },
        ["Siege Ogre"] = new[]
        {
            "Siege walls crumble under my strength. So do YOU!",
            "Brute force. Endless. Unstoppable.",
            "Buckle. Break. SHATTER!"
        },
        ["Blade Dancer"] = new[]
        {
            "Grace and steel — your end is BEAUTIFUL!",
            "I dance circles around you. You cannot follow.",
            "Precision. Momentum. Victory in every stroke."
        },
        ["Mana Leech"] = new[]
        {
            "Your power flows INTO me now!",
            "I grow stronger as you fade.",
            "Empty yourself. Feel the hunger consume you."
        },
        ["Shield Breaker"] = new[]
        {
            "Your defenses are MINE to shatter!",
            "No armor can hold against my resolve.",
            "Strip you bare. Leave you vulnerable. Finish you."
        },
        ["Archlich Sovereign"] = new[]
        {
            "Skeletal guardians empowered by MY magic crush you!",
            "Centuries of conquest echo in this blow.",
            "I am sovereign. You are NOTHING."
        },
        ["Abyssal Leviathan"] = new[]
        {
            "Pressure builds. Your bones creak under the ABYSS!",
            "Depth and darkness are my strengths. You have neither.",
            "The deep rises to claim its due."
        },
        ["Infernal Dragon"] = new[]
        {
            "FLAMES CONSUME! Ash is all you'll leave behind!",
            "I am the fire's fury incarnate!",
            "Your flesh MELTS before true heat!"
        }
    };

    private static readonly Dictionary<string, string[]> _idleTaunts = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Goblin"] = new[]
        {
            "You're still standing? That's... impressive for a human.",
            "Don't bother running. I'm faster than I look.",
            "Boss said not to play with my food. But he's not here."
        },
        ["Skeleton"] = new[]
        {
            "Tick, tock. Your time runs short.",
            "I've worn the bones of your kind before. They crackle nicely.",
            "How long can flesh last against the eternal?"
        },
        ["Troll"] = new[]
        {
            "Troll stronger. Troll only get madder. You understand now?",
            "Your weapons bounce off hide like pebbles. How does that feel?",
            "Keep swinging. Eventually you'll tire. I never do."
        },
        ["Dark Knight"] = new[]
        {
            "Is that your best effort? How... disappointing.",
            "I've fought in wars that would shatter your mind.",
            "Every heartbeat brings you closer to the dark."
        },
        ["Malachar the Undying"] = new[]
        {
            "Centuries turn to dust. So shall you.",
            "Feel my eternal hunger press against your fragile frame.",
            "You struggle like all the others. It changes nothing."
        },
        ["Goblin Shaman"] = new[]
        {
            "The hex whispers your true name. Soon you'll understand.",
            "My magic tastes your fear. It's quite delicious.",
            "You dance for me now, puppet. Isn't it fun?"
        },
        ["Stone Golem"] = new[]
        {
            "Rock does not tire. You will.",
            "Your pathetic flames cannot melt what is eternal.",
            "Stand against me? You already fall."
        },
        ["Wraith"] = new[]
        {
            "The boundary between worlds grows thin for you.",
            "I see through you. Your secrets, your end — all plain.",
            "The cold around me is not coldness. It is emptiness."
        },
        ["Vampire Lord"] = new[]
        {
            "Your pulse quickens. How... tantalizing.",
            "I've watched kingdoms rise and crumble. You won't even be a footnote.",
            "Surrender your lifeblood willingly. It's less painful that way."
        },
        ["Mimic"] = new[]
        {
            "Underneath the lies, I am always HUNGRY.",
            "You wanted treasure so badly. Careful what you wish for.",
            "My form is whatever you want to see. My hunger never lies."
        },
        ["Giant Rat"] = new[]
        {
            "The warren grows every day. Where do you think they come from?",
            "Squeak, squeak... that's the sound of your skull cracking.",
            "Plague rats breed in corpses. Perhaps you'll be next."
        },
        ["Cursed Zombie"] = new[]
        {
            "The curse sings through my rotting sinews.",
            "I'm already dead. What are you?",
            "Your panic smells like rotten meat. I love that smell."
        },
        ["Blood Hound"] = new[]
        {
            "Every wound makes you easier to track.",
            "The pack can taste you from miles away.",
            "Even now, I hear your heartbeat. Thump-thump-thump."
        },
        ["Iron Guard"] = new[]
        {
            "I am the law. I am the order. I am your end.",
            "For the Crown! For the realm! For your DOOM!",
            "I've crushed a thousand rebels less skilled than you."
        },
        ["Night Stalker"] = new[]
        {
            "The darkness loves me. It whispers secrets only I can hear.",
            "You never see the killing blow. That's the beauty of it.",
            "Stay still. It'll be over quickly. Probably."
        },
        ["Frost Wyvern"] = new[]
        {
            "I've slept for ages beneath the ice. Your warmth is... nice.",
            "Frost preserves things perfectly. You'll keep forever.",
            "The blizzard remembers its children. I am its heir."
        },
        ["Chaos Knight"] = new[]
        {
            "Order fractures under my blade. Watch it shatter.",
            "Reality bends when I walk. Can you feel it breaking?",
            "Entropy always wins. And I am entropy's chosen."
        },
        ["Lich King"] = new[]
        {
            "I ruled when your ancestors were dust. I'll rule when you are too.",
            "Magic flows from me like a river of shadow.",
            "Your resistance only proves you have something worth taking."
        },
        ["Shadow Imp"] = new[]
        {
            "We crawl in the darkness. We multiply without end.",
            "One of you. Dozens of us. Do the math, tiny one.",
            "Shadows shimmer and our laughter echoes through the void."
        },
        ["Carrion Crawler"] = new[]
        {
            "The poison drips so slowly. You'll feel it for hours.",
            "Decay is natural. Surrender to it.",
            "My larvae thrive in dead flesh. You're about to be their feast."
        },
        ["Dark Sorcerer"] = new[]
        {
            "Magic is suffering. I've taught myself to love it.",
            "Every spell I weave bends your reality further.",
            "You fight against forces your mind can't comprehend."
        },
        ["Bone Archer"] = new[]
        {
            "I've fired ten thousand arrows. None have missed.",
            "From darkness I wait. From silence I strike.",
            "You think you're safe? My next shot disagrees."
        },
        ["Crypt Priest"] = new[]
        {
            "Death is divine. I am its prophet.",
            "You pray to gods who've forgotten you. I never do.",
            "Judgment is inevitable. It whispers your name."
        },
        ["Plague Bear"] = new[]
        {
            "The sickness is a gift. I give it gladly.",
            "My claws drip with disease. Feast on it.",
            "The rot grows stronger with every breath you take."
        },
        ["Siege Ogre"] = new[]
        {
            "I've brought down castle walls with my bare fists.",
            "Your armor is tissue paper. Your shield is a joke.",
            "Mighty? You're a mosquito. I am the storm."
        },
        ["Blade Dancer"] = new[]
        {
            "Watch me dance. Admire your own inevitable defeat.",
            "Every movement is poetry. Every slash is your doom.",
            "I've perfected the art of death. You're my masterpiece."
        },
        ["Mana Leech"] = new[]
        {
            "I feel your power draining into me. It tastes sweet.",
            "What was yours is now mine. Such is the way of things.",
            "You grow weak. I grow strong. This is justice."
        },
        ["Shield Breaker"] = new[]
        {
            "No defense stands before me. They all break eventually.",
            "I've shattered shields forged by master smiths. Yours is next.",
            "Your walls crumble. Your hope is next."
        },
        ["Archlich Sovereign"] = new[]
        {
            "I command the dead as I command you.",
            "My reign stretches across centuries. Where is yours?",
            "Bow before sovereignty. Or fall before it."
        },
        ["Abyssal Leviathan"] = new[]
        {
            "The deep calls. Soon you'll answer.",
            "Pressure, darkness, cold — your tomb awaits.",
            "Thrash all you like. The abyss does not yield."
        },
        ["Infernal Dragon"] = new[]
        {
            "The flames RISE! Can you feel the heat mounting?",
            "I've incinerated armies. You're just another kindling.",
            "Smoke rises. Ash falls. Your choice is already made."
        }
    };

    private static readonly Dictionary<string, string[]> _desperationLines = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Goblin"] = new[]
        {
            "This isn't how it was supposed to go!",
            "You'll pay for this! You'll ALL pay for this!",
            "Retreat isn't an option... but neither is losing!"
        },
        ["Skeleton"] = new[]
        {
            "The curse weakens! No, NO!",
            "My form... it cannot be! I am ETERNAL!",
            "Not like this. Not at the hands of a mortal!"
        },
        ["Troll"] = new[]
        {
            "Troll body breaks?! Impossible!",
            "Something wrong... can't heal... can't THINK!",
            "RRRAAHHH! FINISH IT IF YOU CAN!"
        },
        ["Dark Knight"] = new[]
        {
            "Impossible. I have not survived this long to fall to YOU!",
            "The darkness... it abandons me?",
            "No! I am the darkness! I cannot—"
        },
        ["Malachar the Undying"] = new[]
        {
            "After all this time... I am dying?",
            "This is not how eternity ends!",
            "No, no, NO! I am the ETERNAL HUNGER!"
        },
        ["Goblin Shaman"] = new[]
        {
            "The hex unravels! My power fades!",
            "This magic can't hold... it's crumbling!",
            "I feel the darkness pulling me down... down..."
        },
        ["Stone Golem"] = new[]
        {
            "Cracks spread... spreading... NO!",
            "My form shatters! The spell breaks!",
            "I... am... collapsing..."
        },
        ["Wraith"] = new[]
        {
            "I'm being pulled back... NO! NOT YET!",
            "The void rejects me. This cannot happen!",
            "I feel myself dispersing... fading into shadow..."
        },
        ["Vampire Lord"] = new[]
        {
            "Centuries of power... WASTED?!",
            "The sun... even in shadow, I feel it closing in...",
            "No! I will not crumble to dust this day!"
        },
        ["Mimic"] = new[]
        {
            "My form unstabilizes... my hunger... fading...",
            "Cannot consume... cannot survive... NO!",
            "My endless hunger meets its match. Impossible!"
        },
        ["Giant Rat"] = new[]
        {
            "The warren... they'll know I fell!",
            "No! The pack depends on me!",
            "SQUEAK! SQUEEEEEEEAK!"
        },
        ["Cursed Zombie"] = new[]
        {
            "The curse weakens... I feel... sensation?",
            "No, no, the binding breaks! I'm becoming... aware!",
            "I can almost remember what it meant to be alive..."
        },
        ["Blood Hound"] = new[]
        {
            "My blood... it burns! What's happening?!",
            "The pack cries out! I hear them!",
            "I'm losing the scent... the hunt is slipping..."
        },
        ["Iron Guard"] = new[]
        {
            "My armor fails me? Impossible!",
            "The Crown's strength... abandoning me?",
            "Stand down? NEVER! I will—"
        },
        ["Night Stalker"] = new[]
        {
            "The shadows... they won't answer me!",
            "The darkness retreats? HOW?!",
            "I'm being drawn into the light... NO!"
        },
        ["Frost Wyvern"] = new[]
        {
            "The ice melts... my power drains...",
            "No! Not to a warm-blooded wretch!",
            "The blizzard's hold breaks... I'm weakening..."
        },
        ["Chaos Knight"] = new[]
        {
            "Order... constricts around me!",
            "The entropy fades! Reality solidifies!",
            "I feel myself pulled toward... coherence?"
        },
        ["Lich King"] = new[]
        {
            "My reign of millennia... ending?",
            "The magic is unstable! The phylactery—",
            "Not this way... not to you!"
        },
        ["Shadow Imp"] = new[]
        {
            "We're dimming! The shadows shriek!",
            "The legion scatters! No, hold together!",
            "We fade... we scatter... we die..."
        },
        ["Carrion Crawler"] = new[]
        {
            "The poison... it consumes me instead!",
            "My form dissolves! The decay accelerates!",
            "Nooooo! The swarm abandons the queen!"
        },
        ["Dark Sorcerer"] = new[]
        {
            "The spell unravels! My power erupts!",
            "The magic consumes itself! I CAN'T CONTROL IT!",
            "Knowledge burns away... burning... burning..."
        },
        ["Bone Archer"] = new[]
        {
            "Impossible! How did you avoid the shot?!",
            "My aim wavers... I'm losing focus!",
            "Everything... darkens..."
        },
        ["Crypt Priest"] = new[]
        {
            "Death rejects me? The judgment... reverses?",
            "I am the vessel... no... the vessel cracks!",
            "The gods... they will not answer!"
        },
        ["Plague Bear"] = new[]
        {
            "My own disease consumes me!",
            "The infection spirals... I feel it eating me alive!",
            "ROOOAAAARRRRR! THE PAIN!"
        },
        ["Siege Ogre"] = new[]
        {
            "My strength... it fades!",
            "The mighty fall... even I?",
            "How can I be broken?! I AM UNBREAKABLE!"
        },
        ["Blade Dancer"] = new[]
        {
            "The rhythm breaks... my dance falters!",
            "My grace... slipping... NO!",
            "Every step is agony now. The dance ends..."
        },
        ["Mana Leech"] = new[]
        {
            "The power reverses! It burns me!",
            "I'm drowning in stolen magic!",
            "Emptiness... the hunger consumes me..."
        },
        ["Shield Breaker"] = new[]
        {
            "YOU'VE BROKEN ME?!",
            "The tables turn... impossible!",
            "I never thought... I could feel this vulnerable..."
        },
        ["Archlich Sovereign"] = new[]
        {
            "The guardians... they crumble!",
            "My sovereignty crumbles with them!",
            "Centuries of rule... ending now?"
        },
        ["Abyssal Leviathan"] = new[]
        {
            "The pressure... it crushes inward!",
            "I surface, gasping... the light burns!",
            "The abyss... it reclaims me..."
        },
        ["Infernal Dragon"] = new[]
        {
            "MY FLAMES SPUTTER?! IMPOSSIBLE!",
            "The inferno dies! The fire fades!",
            "I feel myself cooling... darkening... FALLING!"
        }
    };

    private static readonly string[] _defaultIntro = { "The {0} attacks!" };
    private static readonly string[] _defaultDeath = { "The {0} falls." };
    private static readonly string[] _defaultCritReaction = { "A brutal critical strike lands!" };
    private static readonly string[] _defaultIdleTaunt = { "It circles closer, watching for an opening." };
    private static readonly string[] _defaultDesperationLine = { "It senses its end approaching — and fights harder for it." };

    /// <summary>Returns the pool of encounter introduction lines for the given enemy name.</summary>
    public static string[] GetIntros(string enemyName) =>
        _intros.GetValueOrDefault(enemyName, _defaultIntro);

    /// <summary>Returns the pool of death narration lines for the given enemy name.</summary>
    public static string[] GetDeaths(string enemyName) =>
        _deaths.GetValueOrDefault(enemyName, _defaultDeath);

    /// <summary>Returns the pool of critical hit reaction lines for the given enemy name.</summary>
    public static string[] GetCritReactions(string enemyName) =>
        _critReactions.GetValueOrDefault(enemyName, _defaultCritReaction);

    /// <summary>Returns the pool of mid-combat idle taunt lines for the given enemy name.</summary>
    public static string[] GetIdleTaunts(string enemyName) =>
        _idleTaunts.GetValueOrDefault(enemyName, _defaultIdleTaunt);

    /// <summary>Returns the pool of desperation lines (< 25% HP) for the given enemy name.</summary>
    public static string[] GetDesperationLines(string enemyName) =>
        _desperationLines.GetValueOrDefault(enemyName, _defaultDesperationLine);
}
