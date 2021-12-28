import { ThemeType } from '../controllers/theme';

declare global {
    var CONFIG: {
        address: string
        theme: ThemeType
    };
}