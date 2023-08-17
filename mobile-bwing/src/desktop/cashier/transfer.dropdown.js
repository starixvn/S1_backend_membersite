import React, { Component } from "react";

export default class DropdownBox extends Component {
	constructor(props) {
		super(props);
		this.state = { isOpen: false };
		this.ClickDropdown = this.ClickDropdown.bind(this);
		this.handleClickOutside = this.handleClickOutside.bind(this);
		document.addEventListener("mousedown", this.handleClickOutside);
	}
	handleClickOutside(e) {
		if (this.wrapper && !this.wrapper.contains(e.target)) {
			this.setState({ isOpen: false });
		}
	}
	SetOuterDivRef = node => {
		this.wrapper = node;
	};
	ClickDropdown = () => {
		this.setState({ isOpen: !this.state.isOpen });
	};
	onSelect = v => {
		this.setState({ isOpen: false });
		if (this.props.onSelect !== undefined) {
			this.props.onSelect(v);
		}
	};
	render() {
		let data = this.props.Data;
		let value = this.props.Value;
		let SelectedItem = "";
		let DropdownItem = data.map(
			function (item) {
				return (
					<div
						className="item"
						key={item.Value}
						onClick={this.onSelect.bind(this, item.Value)}>
						<span className="text-bold name">{item.Title}</span>
						<span className="text-bold text-green value">
							{item.DisplayText}
						</span>
					</div>
				);
			}.bind(this)
		);

		if (data != null) {
			for (let i = 0; i < data.length; i++) {
				if (data[i].Value == value) {
					let item = data[i];
					SelectedItem = (
						<>
							<span className="text-bold name">{item.Title}</span>
							<span className="text-bold text-green value">
								{item.DisplayText}
							</span>
						</>
					);
					break;
				}
			}
		}

		return (
			<div className="wallet-group dropdown" ref={this.SetOuterDivRef}>
				<div className="selected-text" onClick={this.ClickDropdown}>
					{SelectedItem}
					<span className="icon-action">
						<i className="icon icon-arrow-down"></i>
					</span>
				</div>
				<div
					className={`dropdown-container${
						this.state.isOpen ? "" : " hide"
					}`}>
					{DropdownItem}
				</div>
			</div>
		);
	}
}
