import React, { useState, useEffect } from "react";
import { useValue } from "cs2/api";
import styles from "../pinboard-panel.module.css";
import { Text } from "../text";
import { CollectionData, DistrictInfo, DistrictScan } from "../types";
import { parse } from "../assets";
import { districts$, districtScan$, listDistricts, pinDistrict, scanDistrict, MAX_COLLECTION_NAME, MAX_FILTER_NAME } from "../game";
import { CheckboxChecked, CheckboxUnchecked, Radio } from "../icons";

// "Pin everything in a district": choose a district, what kinds of things to
// pin from it, and which collection (and optionally filter) to put them in.
// The game side finds what's there and does the pinning.

// The kinds of things to choose from. The ids match the C# code (PinboardUISystem.Districts.cs).
const DISTRICT_CATEGORIES = [
    { id: "buildings", label: "catBuildings" },
    { id: "zoned",     label: "catZoned" },
    { id: "objects",   label: "catObjects" },
    { id: "trees",     label: "catTrees" },
    { id: "networks",  label: "catNetworks" },
] as const;

export const DistrictView = ({ t, collections, onDone }: {
    t: Text; collections: CollectionData[]; onDone: (collectionId: string | null) => void;
}) => {
    const districts = parse<DistrictInfo[]>(useValue(districts$), []);
    const scan      = parse<DistrictScan | null>(useValue(districtScan$), null);

    const [districtId, setDistrictId] = useState<string | null>(null);
    // Everything except zoned buildings to start with: those are grown by the game, not placed.
    const [cats, setCats]             = useState<string[]>(["buildings", "objects", "trees", "networks"]);
    const [colChoice, setColChoice]   = useState<string>("new");
    const [newColName, setNewColName] = useState("");
    const [filterChoice, setFilterChoice] = useState<string>("");
    const [newFilterName, setNewFilterName] = useState("");

    // Fresh list of districts each time this screen opens.
    useEffect(() => { listDistricts(); }, []);

    const chooseDistrict = (d: DistrictInfo) => {
        setDistrictId(d.id);
        if (!newColName) setNewColName(d.name.slice(0, MAX_COLLECTION_NAME));
        scanDistrict(d.id);
    };

    const scanReady = !!districtId && scan?.districtId === districtId;
    const countOf = (cat: string) => scanReady ? (scan!.counts[cat] ?? 0) : 0;
    const total = cats.reduce((sum, c) => sum + countOf(c), 0);
    const allOn = DISTRICT_CATEGORIES.every(c => cats.includes(c.id));

    const toggleCat = (id: string) => setCats(cs => cs.includes(id) ? cs.filter(c => c !== id) : [...cs, id]);
    const toggleAll = () => setCats(allOn ? [] : DISTRICT_CATEGORIES.map(c => c.id));

    const targetCol = collections.find(c => c.id === colChoice) ?? null;
    const canPin = scanReady && total > 0
        && (targetCol !== null || newColName.trim() !== "")
        && (filterChoice !== "new" || newFilterName.trim() !== "");

    const submit = () => {
        if (!canPin || !districtId) return;
        pinDistrict({
            districtId,
            categories: cats,
            collectionId: targetCol?.id ?? null,
            newCollectionName: newColName.trim(),
            filterId: targetCol && filterChoice && filterChoice !== "new" ? filterChoice : null,
            newFilterName: filterChoice === "new" ? newFilterName.trim() : "",
        });
        onDone(targetCol?.id ?? null);
    };

    if (districts.length === 0)
        return <div className={styles.emptyState}>{t("districtNone")}</div>;

    return (
        <div className={styles.districtForm}>
            <div className={styles.formHeading}>{t("districtChoose")}</div>
            {districts.map(d => (
                <div key={d.id} className={styles.checkboxRow} onClick={() => chooseDistrict(d)}>
                    <Radio on={d.id === districtId} />
                    <span>{d.name}</span>
                </div>
            ))}

            {districtId && (
                <>
                    <div className={styles.formHeading}>{t("districtWhat")}</div>
                    {!scanReady && <div className={styles.pickNoLists}>{t("districtLooking")}</div>}
                    <div className={`${styles.checkboxRow} ${allOn ? styles.checkboxRowChecked : ""}`} onClick={toggleAll}>
                        {allOn ? <CheckboxChecked /> : <CheckboxUnchecked />}
                        <span className={styles.formRowLabel}>{t("districtEverything")}</span>
                    </div>
                    {DISTRICT_CATEGORIES.map(c => {
                        const on = cats.includes(c.id);
                        return (
                            <div key={c.id} className={`${styles.checkboxRow} ${styles.formIndent} ${on ? styles.checkboxRowChecked : ""}`} onClick={() => toggleCat(c.id)}>
                                {on ? <CheckboxChecked /> : <CheckboxUnchecked />}
                                <span className={styles.formRowLabel}>{t(c.label)}</span>
                                {scanReady && <span className={styles.formCount}>{countOf(c.id)}</span>}
                            </div>
                        );
                    })}
                    {cats.includes("zoned") && <div className={styles.zonedNote}>{t("zonedNote")}</div>}

                    <div className={styles.formHeading}>{t("districtInto")}</div>
                    <div className={styles.checkboxRow} onClick={() => { setColChoice("new"); setFilterChoice(filterChoice === "new" ? "new" : ""); }}>
                        <Radio on={colChoice === "new"} />
                        <span>{t("districtNewCollection")}</span>
                    </div>
                    {colChoice === "new" && (
                        <input
                            maxLength={MAX_COLLECTION_NAME}
                            value={newColName}
                            onChange={e => setNewColName(e.target.value)}
                            placeholder={t("collectionNamePlaceholder")}
                            className={`${styles.renameInput} ${styles.formInput}`}
                        />
                    )}
                    {collections.map(col => (
                        <div key={col.id} className={styles.checkboxRow} onClick={() => { setColChoice(col.id); setFilterChoice(""); }}>
                            <Radio on={colChoice === col.id} />
                            <span>{col.name}</span>
                        </div>
                    ))}

                    <div className={styles.formHeading}>{t("districtFilter")}</div>
                    <div className={styles.checkboxRow} onClick={() => setFilterChoice("")}>
                        <Radio on={filterChoice === ""} />
                        <span>{t("districtNoFilter")}</span>
                    </div>
                    {(targetCol?.filters ?? []).map(f => (
                        <div key={f.id} className={styles.checkboxRow} onClick={() => setFilterChoice(f.id)}>
                            <Radio on={filterChoice === f.id} />
                            <span>{f.name}</span>
                        </div>
                    ))}
                    <div className={styles.checkboxRow} onClick={() => setFilterChoice("new")}>
                        <Radio on={filterChoice === "new"} />
                        <span>{t("districtNewFilter")}</span>
                    </div>
                    {filterChoice === "new" && (
                        <input
                            maxLength={MAX_FILTER_NAME}
                            value={newFilterName}
                            onChange={e => setNewFilterName(e.target.value)}
                            placeholder={t("filterNamePlaceholder")}
                            className={`${styles.renameInput} ${styles.formInput}`}
                        />
                    )}

                    <div className={styles.hint}>{t("districtExplain")}</div>
                    <button
                        className={`${styles.primaryBtn} ${canPin ? "" : styles.primaryBtnDisabled}`}
                        onClick={submit}
                    >{total === 1 ? t("districtPinButton1") : t("districtPinButton", total)}</button>
                </>
            )}
        </div>
    );
};
