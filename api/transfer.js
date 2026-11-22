// MODULE TRANSFER — Lumio API
const TRANSFER = {
    repoOwner: "Nexora-Interactive-Technologies-Lumio",
    repoName: "Lumio",
    token: "TON_GITHUB_TOKEN_ICI", // Tu le mettras plus tard pour le Récepteur
    lastCmdId: localStorage.getItem("lumio_cmd") || "",

    async sendTelemetry(currentGame) {
        let bat = "Inconnu";
        try {
            if (navigator.getBattery) {
                const b = await navigator.getBattery();
                bat = Math.round(b.level * 100) + "%";
            }
        } catch (e) {}

        const payload = {
            appareil: navigator.platform || "iPadOS",
            batterie: bat,
            reseau: navigator.onLine ? "Wi-Fi Actif" : "Hors ligne",
            jeu_actif: currentGame,
            heure: new Date().toLocaleTimeString("fr-CA")
        };

        localStorage.setItem("lumio_last_telemetry", JSON.stringify(payload));
        if (this.token === "TON_GITHUB_TOKEN_ICI") return;

        try {
            const url = `https://github.com{this.repoOwner}/${this.repoName}/contents/api/telemetry.json`;
            const getRes = await fetch(url, { headers: { Authorization: `Bearer ${this.token}` } });
            const fileData = await getRes.json();

            await fetch(url, {
                method: "PUT",
                headers: { Authorization: `Bearer ${this.token}`, "Content-Type": "application/json" },
                body: JSON.stringify({
                    message: "Update telemetry",
                    content: btoa(unescape(encodeURIComponent(JSON.stringify(payload, null, 2)))),
                    sha: fileData.sha
                })
            });
        } catch (e) {}
    },

    async checkCommands(onCommand) {
        try {
            const res = await fetch("commands.json?t=" + Date.now());
            const cmd = await res.json();
            if (cmd.command_id && cmd.command_id !== this.lastCmdId) {
                this.lastCmdId = cmd.command_id;
                localStorage.setItem("lumio_cmd", cmd.command_id);
                onCommand(cmd);
            }
        } catch (e) {}
    }
};
