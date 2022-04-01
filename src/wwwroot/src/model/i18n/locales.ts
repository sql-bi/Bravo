/*!
 * Bravo for Power BI
 * Copyright (c) SQLBI corp. - All rights reserved.
 * https://www.sqlbi.com
*/

import en from './en';
//import es from './es';
//import pt_BR from './pt-BR';
import fr from './fr';
//import de from './de';
import it from './it';
//import pl from './pl';
import ru from './ru';
//import zh_CN from './zh-CN';
//import zh_TW from './zh-TW';

const locales = { 
    [en.locale]: en, 
    //[es.locale]: es, 
    //[pt_BR.locale]: pt_BR,
    [fr.locale]: fr,  
    //[de.locale]: de, 
    [it.locale]: it,
    //[pl.locale]: pl,
    [ru.locale]: ru,
    //[zh_CN.locale]: zh_CN,
    //[zh_TW.locale]: zh_TW,    
}; 
export default locales;
