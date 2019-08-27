package com.microsoft.bot.builder.solutions.eventcompanion

import android.content.Context
import android.util.AttributeSet
import android.widget.Spinner

class MySpinner(context: Context, attributeSet: AttributeSet) : Spinner(context, attributeSet) {
    private lateinit var listener: OnItemSelectedListener
    override fun setSelection(position: Int) {
        super.setSelection(position)
        if (position == selectedItemPosition) {
            listener.onItemSelected(null, null, position, 0)
        }
    }

    override fun setOnItemSelectedListener(listener: OnItemSelectedListener?) {
        if (listener != null) {
            this.listener = listener
        }
    }
}