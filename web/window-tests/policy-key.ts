import { execFile } from 'node:child_process';
import { randomUUID } from 'node:crypto';
import { promisify } from 'node:util';
import { reg } from './windows-tools';

// The policies of the application of a test, in a key of that test under HKEY_CURRENT_USER, which a development
// build reads in place of the keys of the policies when its command line names it (--dev-policy-key). No test
// writes the policies of the machine or of the user.

const execFileAsync = promisify(execFile);

/** The values of the policies by their names: a number is written as a DWORD, a text as a string. */
export type PolicyValues = Record<string, number | string>;

const parentName = 'Software\\Bravo.Tests';

export class PolicyKey {

    /** The path of the key under HKEY_CURRENT_USER, as --dev-policy-key takes it. */
    readonly name = `${parentName}\\${randomUUID().replace(/-/g, '')}`;

    private constructor() { }

    /** Writes the values in a new key. */
    static async write(values: PolicyValues): Promise<PolicyKey> {
        const key = new PolicyKey();
        try {
            for (const [name, value] of Object.entries(values)) {
                const kind = (typeof value === 'number' ? 'REG_DWORD' : 'REG_SZ');
                await execFileAsync(reg, ['add', `HKCU\\${key.name}`, '/v', name, '/t', kind, '/d', String(value), '/f']);
            }
        } catch (error) {
            await key.remove();
            throw error;
        }

        return key;
    }

    /** Removes the key, and the key of the tests when nothing else is left in it. */
    async remove() {
        await execFileAsync(reg, ['delete', `HKCU\\${this.name}`, '/f']).catch(() => { });

        // reg lists the key itself, then each of its values and subkeys on a line of its own
        const { stdout } = await execFileAsync(reg, ['query', `HKCU\\${parentName}`]).catch(() => ({ stdout: '' }));
        if (stdout.split(/\r?\n/).filter(line => line.trim()).length === 1)
            await execFileAsync(reg, ['delete', `HKCU\\${parentName}`, '/f']).catch(() => { });
    }
}
