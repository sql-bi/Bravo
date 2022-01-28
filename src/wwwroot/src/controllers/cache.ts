/*!
 * Bravo for Power BI
 * Copyright (c) SQLBI corp. - All rights reserved.
 * https://www.sqlbi.com
*/

import { Dic } from '../helpers/utils';

export class CacheHelper{

    storageName: string;

    constructor(storageName: string) {
        this.storageName = storageName;
    }

    getCache<T>(): Dic<T> {

        let cache = {};
        const rawData = localStorage.getItem(this.storageName);
        if (rawData) {
            try {
                cache = JSON.parse(rawData);
            } catch(error){}
        }
        return cache;
    }

    saveCache<T>(cache: Dic<T>) {
        try {
            localStorage.setItem(this.storageName, JSON.stringify(cache));
        } catch(error){}
    }

    setItem<T>(id: string, item: T) {

        let cache = this.getCache<T>();
        cache[id] = item;
        this.saveCache(cache);
    }

    getItem<T>(id: string): T {
        let cache = this.getCache<T>();
        if (id in cache)
            return cache[id];

        return null;
    }

    removeItem(id: string) {
        let cache = this.getCache();
        delete cache[id];
        this.saveCache(cache);
    }


}