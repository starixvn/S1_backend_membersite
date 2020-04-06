import {DropdownBox} from './dropDownBox';

export const QuickPickDefs = {
    Today: {value: 'Today', displayText:i18n.lbl_Today},
    YesterDay: {value: 'YesterDay', displayText:i18n.lbl_YesterDay},
    ThisWeek: {value: 'ThisWeek', displayText:i18n.lbl_ThisWeek},
    LastWeek: {value: 'LastWeek', displayText:i18n.lbl_LastWeek},
    ThisMonth: {value: 'ThisMonth', displayText:i18n.lbl_ThisMonth},
    LastMonth: {value: 'LastMonth', displayText:i18n.lbl_LastMonth},
}

export const QuickPicker = (props)=>{
    return(
        <DropdownBox 
            OnSelectItem = {props.QuickPickOnSelect}
            DataColumn = "QuickPick"
            ColumnTitle = {i18n.lbl_QuickPick}
            Options = {Object.keys(QuickPickDefs).map(key=>{return {...QuickPickDefs[key]}})}
            SelectedItem = {props.QuickPick}
        />
    )
}

