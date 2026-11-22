// MOTEUR 3D LÉGER (Three.js) — Lumio Playground
let scene, camera, renderer, animId;

function stop3DGame() {
    if (animId) cancelAnimationFrame(animId);
    const box = document.getElementById("threeContainer");
    if (box) box.innerHTML = "";
}

function init3DScene(containerId, bgColor) {
    stop3DGame();
    const container = document.getElementById(containerId);
    scene = new THREE.Scene();
    scene.background = new THREE.Color(bgColor);
    camera = new THREE.PerspectiveCamera(60, container.clientWidth / 300, 0.1, 100);
    renderer = new THREE.WebGLRenderer({ antialias: true });
    renderer.setSize(container.clientWidth, 300);
    container.appendChild(renderer.domElement);

    const light = new THREE.DirectionalLight(0xffffff, 1.2);
    light.position.set(5, 10, 7);
    scene.add(light, new THREE.AmbientLight(0x404040, 1.5));
}

// 1. BUS ESCAPE 3D (Style Subway Surfers — Déterminants)
function startBusEscape3D(onWin) {
    init3DScene("threeContainer", 0x06080d);
    camera.position.set(0, 4.5, 7);
    camera.lookAt(0, 0, -5);

    const road = new THREE.Mesh(new THREE.BoxGeometry(6, 0.2, 40), new THREE.MeshStandardMaterial({ color: 0x1e293b }));
    road.position.z = -12;
    scene.add(road);

    const player = new THREE.Mesh(new THREE.BoxGeometry(0.8, 1.2, 0.8), new THREE.MeshStandardMaterial({ color: 0x6366f1 }));
    player.position.set(0, 0.7, 2);
    scene.add(player);

    let lane = 0, items = [], caught = 0, frame = 0;
    window.move3DLane = (dir) => { lane = Math.max(-1, Math.min(1, lane + dir)); player.position.x = lane * 1.8; };

    function loop() {
        frame++;
        if (frame % 45 === 0) {
            const l = [-1, 0, 1][Math.floor(Math.random() * 3)];
            const isGood = Math.random() > 0.45;
            const geo = isGood ? new THREE.OctahedronGeometry(0.55) : new THREE.BoxGeometry(1.3, 1.4, 2.4);
            const mat = new THREE.MeshStandardMaterial({ color: isGood ? 0x10b981 : 0xef4444 });
            const mesh = new THREE.Mesh(geo, mat);
            mesh.position.set(l * 1.8, 0.8, -22);
            scene.add(mesh);
            items.push({ mesh, lane: l, isGood });
        }

        for (let i = items.length - 1; i >= 0; i--) {
            let it = items[i];
            it.mesh.position.z += 0.28;
            if (it.isGood) it.mesh.rotation.y += 0.08;

            if (it.lane === lane && Math.abs(it.mesh.position.z - 2) < 1.1) {
                if (it.isGood) {
                    caught++;
                    document.getElementById("gameHudText").innerText = `Cible : "___ pomme" (une) | Déterminants : ${caught}/3`;
                    if (caught >= 3) { stop3DGame(); if (onWin) onWin(); return; }
                }
                scene.remove(it.mesh);
                items.splice(i, 1);
                continue;
            }
            if (it.mesh.position.z > 6) { scene.remove(it.mesh); items.splice(i, 1); }
        }
        renderer.render(scene, camera);
        animId = requestAnimationFrame(loop);
    }
    loop();
}
// 2. GRAMMAR OPS 3D (Style Call of Duty — Bots + Loadout max 4)
function startGrammarOps3D(onWin) {
    init3DScene("threeContainer", 0x090d16);
    camera.position.set(0, 3.2, 6.5);
    camera.lookAt(0, 1, -4);

    const floor = new THREE.Mesh(new THREE.PlaneGeometry(18, 18), new THREE.MeshStandardMaterial({ color: 0x111827 }));
    floor.rotation.x = -Math.PI / 2;
    scene.add(floor);

    const allWeapons = ["Pistolet Tactique", "Fusil d'Assaut", "Sniper Laser", "Drone Recon"];
    const nouns = ["Québec", "chat", "école", "avion", "maison", "soleil"];
    let loadout = ["Couteau"];
    let targets = [], hits = 0;

    function spawnNounBot() {
        const word = nouns[Math.floor(Math.random() * nouns.length)];
        const bot = new THREE.Mesh(new THREE.CylinderGeometry(0.5, 0.5, 1.6, 12), new THREE.MeshStandardMaterial({ color: 0x22d3ee }));
        bot.position.set((Math.random() - 0.5) * 7, 0.8, -2 - Math.random() * 6);
        bot.userData = { word };
        scene.add(bot);
        targets.push(bot);
        document.getElementById("gameHudText").innerText = `Cible Nom : "${word}" | Loadout (${loadout.length}/4) : ${loadout.join(", ")}`;
    }

    for (let i = 0; i < 3; i++) spawnNounBot();

    window.shootOpsBot = () => {
        if (targets.length === 0) return;
        const hitBot = targets.pop();
        scene.remove(hitBot);
        hits++;
        if (loadout.length < 4) loadout.push(allWeapons[loadout.length - 1]);
        if (hits >= 4) { stop3DGame(); if (onWin) onWin(); return; }
        spawnNounBot();
    };

    function loop() {
        targets.forEach(b => { b.rotation.y += 0.04; });
        renderer.render(scene, camera);
        animId = requestAnimationFrame(loop);
    }
    loop();
}

// 3. REGRAM 3D (Style Roblox Rivals 1v1 en ligne)
function startRegram1v13D() {
    const hud = document.getElementById("gameHudText");
    hud.innerText = "🌐 En attente d'un adversaire (Matchmaking 1v1)...";
    init3DScene("threeContainer", 0x0b1120);
    camera.position.set(0, 4, 7);
    camera.lookAt(0, 1, 0);

    const arena = new THREE.Mesh(new THREE.BoxGeometry(10, 0.3, 10), new THREE.MeshStandardMaterial({ color: 0x1e293b }));
    scene.add(arena);

    // Avatar style Roblox (bloc)
    const p1 = new THREE.Mesh(new THREE.BoxGeometry(0.9, 1.5, 0.6), new THREE.MeshStandardMaterial({ color: 0x3b82f6 }));
    p1.position.set(-2, 0.9, 1);
    scene.add(p1);

    let rival = null, rivalHp = 100;
    setTimeout(() => {
        hud.innerText = "⚔️ Adversaire trouvé : Joueur_QC99 ! Tire pour gagner le duel 1v1 !";
        rival = new THREE.Mesh(new THREE.BoxGeometry(0.9, 1.5, 0.6), new THREE.MeshStandardMaterial({ color: 0xef4444 }));
        rival.position.set(2, 0.9, -1);
        scene.add(rival);
    }, 2500);

    window.shootRegramRival = () => {
        if (!rival) return;
        rivalHp -= 34;
        rival.position.x = (Math.random() - 0.5) * 4;
        if (rivalHp <= 0) {
            hud.innerText = "🏆 VICTOIRE 1v1 REGRAM ! (+250 XP)";
            scene.remove(rival);
            rival = null;
        } else {
            hud.innerText = `⚔️ Duel 1v1 — PV Adversaire : ${rivalHp}%`;
        }
    };

    function loop() {
        if (rival) rival.rotation.y += 0.05;
        renderer.render(scene, camera);
        animId = requestAnimationFrame(loop);
    }
    loop();
}
